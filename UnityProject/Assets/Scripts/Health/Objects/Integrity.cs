using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Object = System.Object;

/// <summary>
/// Component which allows an object to have an integrity value (basically an object's version of HP),
/// take damage, and do things in response to integrity changes. Objects are destroyed when their integrity
/// reaches 0.
///
/// This stuff is tracked server side only, client is informed only when the effects of integrity
/// changes occur.
/// </summary>
[RequireComponent(typeof(CustomNetTransform))]
[RequireComponent(typeof(RegisterTile))]
[RequireComponent(typeof(Meleeable))]
public class Integrity : NetworkBehaviour, IFireExposable, IRightClickable, IOnStageServer
{

	/// <summary>
	/// Server-side event invoked when object integrity reaches 0 by any means and object
	/// destruction logic is about to be invoked. Does not override the default destruction logic,
	/// simply provides a hook for when it is going to be invoked.
	/// </summary>
	[NonSerialized]
	public DestructionEvent OnWillDestroyServer = new DestructionEvent();

	/// <summary>
	/// Server-side burn up logic - invoked when integrity reaches 0 due to burn damage.
	/// Setting this will override the default burn up logic.
	/// See OnWillDestroyServer if you only want an event when the object is destroyed
	/// and don't want to override the burn up logic.
	/// </summary>
	/// <returns></returns>
	[NonSerialized]
	public UnityAction<DestructionInfo> OnBurnUpServer;

	/// <summary>
	/// Armor for this object.
	/// </summary>
	[Tooltip("Armor for this object.")]
	public Armor Armor = new Armor();

	/// <summary>
	/// resistances for this object.
	/// </summary>
	[Tooltip("Resistances of this object.")]
	public Resistances Resistances = new Resistances();

	/// <summary>
	/// Below this temperature, the object will take no damage from fire or heat and won't ignite.
	/// </summary>
	[Tooltip("Below this temperature, the object will take no damage from fire or heat and" +
	         " won't ignite.")]
	public float HeatResistance = 100;

	public float initialIntegrity = 100f;

	[SyncVar(hook = nameof(SyncOnFire))]
	private bool onFire = false;
	private BurningOverlay burningObjectOverlay;

	//TODO: Should probably replace the burning effect with a particle effect?
	private static GameObject SMALL_BURNING_PREFAB;
	private static GameObject LARGE_BURNING_PREFAB;


	// damage incurred each tick while an object is on fire
	private static float BURNING_DAMAGE = 5;

	private static readonly float BURN_RATE = 1f;
	private float timeSinceLastBurn;

	private float integrity = 100f;
	private bool destroyed = false;
	private DamageType lastDamageType;
	private RegisterTile registerTile;

	//whether this is a large object (meaning we would use the large ash pile and large burning sprite)
	private bool isLarge;

	private void Awake()
	{
		if (SMALL_BURNING_PREFAB == null)
		{
			SMALL_BURNING_PREFAB = Resources.Load<GameObject>("SmallBurning");
			LARGE_BURNING_PREFAB = Resources.Load<GameObject>("LargeBurning");
		}
		registerTile = GetComponent<RegisterTile>();
		//this is just a guess - large items can't be picked up
		isLarge = GetComponent<Pickupable>() == null;
		if (Resistances.Flammable)
		{
			if (burningObjectOverlay == false)
			{
				burningObjectOverlay = GameObject.Instantiate(isLarge ? LARGE_BURNING_PREFAB : SMALL_BURNING_PREFAB, transform)
					.GetComponent<BurningOverlay>();
			}

			burningObjectOverlay.enabled = true;
			burningObjectOverlay.StopBurning();
		}
	}

	public void GoingOnStageServer(OnStageInfo info)
	{
		if (info.IsCloned)
		{
			//cloned
			var clonedIntegrity = info.ClonedFrom.GetComponent<Integrity>();
			integrity = clonedIntegrity.integrity;
			timeSinceLastBurn = clonedIntegrity.timeSinceLastBurn;
			destroyed = clonedIntegrity.destroyed;
			SyncOnFire(clonedIntegrity.onFire);
		}
		else
		{
			//spawned
			integrity = initialIntegrity;
			timeSinceLastBurn = 0;
			destroyed = false;
			if (burningObjectOverlay != null)
			{
				burningObjectOverlay.StopBurning();
			}
			SyncOnFire(false);
		}
	}

	public override void OnStartClient()
	{
		SyncOnFire(onFire);
	}

	/// <summary>
	/// Directly deal damage to this object.
	/// </summary>
	/// <param name="damage"></param>
	/// <param name="damageType"></param>
	[Server]
	public void ApplyDamage(float damage, AttackType attackType, DamageType damageType)
	{
		//already destroyed, don't apply damage
		if (destroyed || Resistances.Indestructable) return;

		if (Resistances.FireProof && attackType == AttackType.Fire) return;

		damage = Armor.GetDamage(damage, attackType);
		if (damage > 0)
		{
			if (attackType == AttackType.Fire && !onFire && !destroyed && Resistances.Flammable)
			{
				SyncOnFire(true);
			}
			integrity -= damage;
			lastDamageType = damageType;
			CheckDestruction();
			Logger.LogTraceFormat("{0} took {1} {2} damage from {3} attack (resistance {4}) (integrity now {5})", Category.Health, name, damage, damageType, attackType, Armor.GetRating(attackType), integrity);
		}
	}

	private void Update()
	{
		if (onFire && isServer)
		{
			timeSinceLastBurn += Time.deltaTime;
			if (timeSinceLastBurn > BURN_RATE)
			{
				ApplyDamage(BURNING_DAMAGE, AttackType.Fire, DamageType.Burn);
				timeSinceLastBurn = 0;
			}
		}
	}

	private void SyncOnFire(bool onFire)
	{
		//do nothing if this can't burn
		if (!Resistances.Flammable) return;

		this.onFire = onFire;
		if (this.onFire)
		{
			burningObjectOverlay.Burn();
		}
		else if (!this.onFire)
		{
			burningObjectOverlay.StopBurning();
		}
	}

	[Server]
	private void CheckDestruction()
	{
		if (!destroyed && integrity <= 0)
		{
			var destructInfo = new DestructionInfo(lastDamageType);
			OnWillDestroyServer.Invoke(destructInfo);

			if (onFire)
			{
				//ensure we stop burning
				SyncOnFire(false);
			}

			if (destructInfo.DamageType == DamageType.Burn)
			{
				if (OnBurnUpServer != null)
				{
					OnBurnUpServer(destructInfo);
				}
				else
				{
					DefaultBurnUp(destructInfo);
				}
			}
			else
			{
				DefaultDestroy(destructInfo);
			}

			destroyed = true;
		}
	}

	[Server]
	private void DefaultBurnUp(DestructionInfo info)
	{
		//just a guess - objects which can be picked up should have a smaller amount of ash
		EffectsFactory.Instance.Ash(registerTile.WorldPosition.To2Int(), isLarge);
		Chat.AddLocalMsgToChat($"{name} burnt to ash.", gameObject.TileWorldPosition());
		Logger.LogTraceFormat("{0} burning up, onfire is {1} (burningObject enabled {2})", Category.Health, name, this.onFire, burningObjectOverlay?.enabled);
		PoolManager.PoolNetworkDestroy(gameObject);
	}

	[Server]
	private void DefaultDestroy(DestructionInfo info)
	{
		if (info.DamageType == DamageType.Brute)
		{
			Chat.AddLocalMsgToChat($"{name} was smashed to pieces.", gameObject.TileWorldPosition());
			PoolManager.PoolNetworkDestroy(gameObject);
		}
		//TODO: Other damage types (acid)
		else
		{
			Chat.AddLocalMsgToChat($"{name} was destroyed.", gameObject.TileWorldPosition());
			PoolManager.PoolNetworkDestroy(gameObject);
		}
	}

	[Server]
	public void OnExposed(FireExposure exposure)
	{
		if (exposure.Temperature > HeatResistance)
		{
			ApplyDamage(exposure.StandardDamage(), AttackType.Fire, DamageType.Burn);
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		return RightClickableResult.Create()
			.AddAdminElement("Smash", AdminSmash)
			.AddAdminElement("Hotspot", AdminMakeHotspot);
	}

	private void AdminSmash()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdAdminSmash(gameObject);
	}
	private void AdminMakeHotspot()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdAdminMakeHotspot(gameObject);
	}
}

/// <summary>
/// Contains info on how an object was destroyed
/// </summary>
public class DestructionInfo
{
	/// <summary>
	/// Damage that destroyed the object
	/// </summary>
	public readonly DamageType DamageType;

	public DestructionInfo(DamageType damageType)
	{
		DamageType = damageType;
	}
}

/// <summary>
/// Event fired when an object is destroyed
/// </summary>
public class DestructionEvent : UnityEvent<DestructionInfo>{}
