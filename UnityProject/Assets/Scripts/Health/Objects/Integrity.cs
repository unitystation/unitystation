using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Atmospherics;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Tilemaps.Behaviours.Meta;
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
public class Integrity : NetworkBehaviour, IHealth, IFireExposable, IRightClickable, IServerSpawn
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
	/// Below this temperature (in Kelvin) the object will be unaffected by fire exposure.
	/// </summary>
	[Tooltip("Below this temperature (in Kelvin) the object will be unaffected by fire exposure.")]
	public float HeatResistance = 100;

	public float initialIntegrity = 100f;

	[SyncVar(hook = nameof(SyncOnFire))]
	private bool onFire = false;
	private BurningOverlay burningObjectOverlay;

	//TODO: Should probably replace the burning effect with a particle effect?
	private static GameObject SMALL_BURNING_PREFAB;
	private static GameObject LARGE_BURNING_PREFAB;


	// damage incurred each tick while an object is on fire
	private static float BURNING_DAMAGE = 0.08f;

	private static readonly float BURN_RATE = 1f;
	private float timeSinceLastBurn;

	public float integrity { get; private set; } = 100f;
	private bool destroyed = false;
	private DamageType lastDamageType;
	private RegisterTile registerTile;
	private IPushable pushable;

	//whether this is a large object (meaning we would use the large ash pile and large burning sprite)
	private bool isLarge;

	public float Resistance => pushable == null ? integrity : integrity * ((int)pushable.Size/10f);

	private void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		if (SMALL_BURNING_PREFAB == null)
		{
			SMALL_BURNING_PREFAB = Resources.Load<GameObject>("SmallBurning");
			LARGE_BURNING_PREFAB = Resources.Load<GameObject>("LargeBurning");
		}
		registerTile = GetComponent<RegisterTile>();
		pushable = GetComponent<IPushable>();
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

	public void OnSpawnServer(SpawnInfo info)
	{
		if (info.SpawnType == SpawnType.Clone)
		{
			//cloned
			var clonedIntegrity = info.ClonedFrom.GetComponent<Integrity>();
			integrity = clonedIntegrity.integrity;
			timeSinceLastBurn = clonedIntegrity.timeSinceLastBurn;
			destroyed = clonedIntegrity.destroyed;
			SyncOnFire(onFire, clonedIntegrity.onFire);
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
			SyncOnFire(onFire, false);
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
		SyncOnFire(onFire, onFire);
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
				SyncOnFire(onFire, true);
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

	private void SyncOnFire(bool wasOnFire, bool onFire)
	{
		EnsureInit();
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
			var destructInfo = new DestructionInfo(lastDamageType, this);
			OnWillDestroyServer.Invoke(destructInfo);

			if (onFire)
			{
				//ensure we stop burning
				SyncOnFire(onFire, false);
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
		EffectsFactory.Ash(registerTile.WorldPosition.To2Int(), isLarge);
		Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " burnt to ash.", gameObject.TileWorldPosition());
		Logger.LogTraceFormat("{0} burning up, onfire is {1} (burningObject enabled {2})", Category.Health, name, this.onFire, burningObjectOverlay?.enabled);
		Despawn.ServerSingle(gameObject);
	}

	[Server]
	private void DefaultDestroy(DestructionInfo info)
	{
		if (info.DamageType == DamageType.Brute)
		{
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " got smashed to pieces.", gameObject.TileWorldPosition());
			Despawn.ServerSingle(gameObject);
		}
		//TODO: Other damage types (acid)
		else
		{
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " got destroyed.", gameObject.TileWorldPosition());
			Despawn.ServerSingle(gameObject);
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
		if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken))
		{
			return null;
		}

		return RightClickableResult.Create()
			.AddAdminElement("Smash", AdminSmash)
			.AddAdminElement("Hotspot", AdminMakeHotspot);
	}

	private void AdminSmash()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdAdminSmash(gameObject, ServerData.UserID, PlayerList.Instance.AdminToken);
	}
	private void AdminMakeHotspot()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdAdminMakeHotspot(gameObject, ServerData.UserID, PlayerList.Instance.AdminToken);
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

	/// <summary>
	/// Integrity of the object that was destroyed.
	/// </summary>
	public readonly Integrity Destroyed;

	public DestructionInfo(DamageType damageType, Integrity destroyed)
	{
		DamageType = damageType;
		Destroyed = destroyed;
	}
}

/// <summary>
/// Event fired when an object is destroyed
/// </summary>
public class DestructionEvent : UnityEvent<DestructionInfo>{}
