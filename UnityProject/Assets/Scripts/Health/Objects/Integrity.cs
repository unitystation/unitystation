using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using Mirror;
using Core.Editor.Attributes;
using AddressableReferences;
using AdminCommands;
using DatabaseAPI;
using Effects.Overlays;
using InGameEvents;
using Logs;
using Messages.Client.DevSpawner;
using ScriptableObjects;
using Systems.Atmospherics;
using Systems.Explosions;
using Systems.Interaction;


/// <summary>
/// Component which allows an object to have an integrity value (basically an object's version of HP),
/// take damage, and do things in response to integrity changes. Objects are destroyed when their integrity
/// reaches 0.
///
/// This stuff is tracked server side only, client is informed only when the effects of integrity
/// changes occur.
/// </summary>
///
[RequireComponent(typeof(RegisterTile))]
public class Integrity : NetworkBehaviour, IHealth, IFireExposable, IRightClickable, IServerSpawn, IExaminable, IServerDespawn
{

	/// <summary>
	/// Server-side event invoked when object integrity reaches 0 by any means and object
	/// destruction logic is about to be invoked. Does not override the default destruction logic,
	/// simply provides a hook for when it is going to be invoked.
	/// </summary>
	[NonSerialized]
	public DestructionEvent OnWillDestroyServer = new DestructionEvent();


	/// <summary>
	/// Works on client and server , triggered when onDestroyed is called
	/// </summary>
	public event Action BeingDestroyed;

	/// <summary>
	/// Server-side event invoked when ApplyDamage is called
	/// and Integrity is about to apply damage.
	/// </summary>
	[NonSerialized]
	public DamagedEvent OnApplyDamage = new DamagedEvent();

	/// <summary>
	/// event for hotspots
	/// </summary>
	[NonSerialized]
	public UnityEvent OnExposedEvent = new UnityEvent();

	/// <summary>
	/// Server-side burn up logic - invoked when integrity reaches 0 due to burn damage.
	/// Setting this will override the default burn up logic.
	/// See OnWillDestroyServer if you only want an event when the object is destroyed
	/// and don't want to override the burn up logic.
	/// </summary>
	/// <returns></returns>
	[NonSerialized]
	public UnityAction<DestructionInfo> OnBurnUpServer;

	public Action OnServerDespawnEvent;

	[Tooltip("This object's initial \"HP\"")]
	public float initialIntegrity = 100f;

	//  Commented out as it doesnt work correctly
	[Tooltip("Sound to play when damage applied.")]
	public AddressableAudioSource soundOnHit;


	[Tooltip("A damage threshold the attack needs to pass in order to apply damage to this item.")]
	public float damageDeflection = 0;

	/// <summary>
	/// Armor for this object.
	/// </summary>
	[Tooltip("Armor for this object.")]
	public Armor Armor = new Armor();

	/// <summary>
	/// resistances for this object.
	/// </summary>
	//  Commented out as it doesnt work correctly
	[Tooltip("Resistances of this object.")]
	public Resistances Resistances = new Resistances();

	/// <summary>
	/// Below this temperature (in Kelvin) the object will be unaffected by fire exposure.
	/// </summary>

	[Tooltip("Below this temperature (in Kelvin) the object will be unaffected by fire exposure.")]
	public float HeatResistance = 100;

	/// <summary>
	/// The explosion strength of this object if is set to explode on destroy
	/// </summary>

	[Tooltip("The explosion strength of this object if is set to explode on destroy")]
	public float ExplosionsDamage = 100f;

	[SerializeField ]
	private bool doDamageMessage = true;

	public bool DoDamageMessage => doDamageMessage;

	[SyncVar(hook = nameof(SyncOnFire))]
	private bool onFire = false;
	private BurningOverlay burningObjectOverlay;

	//TODO: Should probably replace the burning effect with a particle effect?
	private static GameObject SMALL_BURNING_PREFAB;
	private static GameObject LARGE_BURNING_PREFAB;

	private static OverlayTile SMALL_ASH;
	private static OverlayTile LARGE_ASH;

	// damage incurred each tick while an object is on fire
	private static float BURNING_DAMAGE = 0.04f;

	private static readonly float BURN_RATE = 1f;

	public float integrity { get; private set; } = 100f;
	private bool destroyed = false;
	private DamageType lastDamageType;
	private RegisterTile registerTile;
	public RegisterTile RegisterTile => registerTile;
	private UniversalObjectPhysics universalObjectPhysics;

	private Meleeable meleeable;
	public Meleeable Meleeable => meleeable;

	//The current integrity divided by the initial integrity
	public float PercentageDamaged => integrity.Approx(0) ? 0 : integrity / initialIntegrity;

	//whether this is a large object (meaning we would use the large ash pile and large burning sprite)
	private bool isLarge;
	public float Resistance => integrity * ((int)universalObjectPhysics.GetSize() / 10f);

	private void Awake()
	{
		EnsureInit();
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer && onFire)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdateBurn);
		}
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		if (SMALL_BURNING_PREFAB == null)
		{
			SMALL_BURNING_PREFAB = CommonPrefabs.Instance.BurningSmall;
			LARGE_BURNING_PREFAB = CommonPrefabs.Instance.BurningLarge;
		}

		if (SMALL_ASH == null)
		{
			SMALL_ASH = TileManager.GetTile(TileType.Effects, "SmallAsh") as OverlayTile;
			LARGE_ASH = TileManager.GetTile(TileType.Effects, "LargeAsh") as OverlayTile;
		}
		meleeable = GetComponent<Meleeable>();
		registerTile = GetComponent<RegisterTile>();
		universalObjectPhysics = GetComponent<UniversalObjectPhysics>();

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
			destroyed = clonedIntegrity.destroyed;
			SyncOnFire(onFire, clonedIntegrity.onFire);
		}
		else
		{
			//spawned
			integrity = initialIntegrity;
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
	public void ApplyDamage(float damage, AttackType attackType, DamageType damageType, bool ignoreDeflection = false, bool triggerEvent = true, bool ignoreArmor = false,
		bool explodeOnDestroy = false)
	{
		//already destroyed, don't apply damage
		if (destroyed || Resistances.Indestructable || (!ignoreDeflection && damage < damageDeflection)) return;

		if (Resistances.FireProof && attackType == AttackType.Fire) return;

		var damageInfo = new DamageInfo(damage, attackType, damageType, this);

		damage = ignoreArmor ? damage : Armor.GetDamage(damage, attackType);
		if (damage > 0)
		{
			if (attackType == AttackType.Fire && !onFire && !destroyed && Resistances.Flammable)
			{
				SyncOnFire(onFire, true);
			}
			integrity -= damage;
			lastDamageType = damageType;

			if (triggerEvent)
			{
				OnApplyDamage.Invoke(damageInfo);
			}

			CheckDestruction(explodeOnDestroy);

			Loggy.LogTraceFormat("{0} took {1} {2} damage from {3} attack (resistance {4}) (integrity now {5})", Category.Damage, name, damage, damageType, attackType, Armor.GetRating(attackType), integrity);
		}
	}

	/// <summary>
	/// Directly restore integrity to this object. Final integrity will not exceed the initial integrity.
	/// </summary>
	[Server]
	public void RestoreIntegrity(float amountToRestore)
	{
		integrity += amountToRestore;
		if (integrity > initialIntegrity)
		{
			integrity = initialIntegrity;
		}
	}

	private void PeriodicUpdateBurn()
	{
		//Instantly stop burning if there's no oxygen at this location
		MetaDataNode node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile.LocalPositionServer);
		if (node?.GasMix.GetMoles(Gas.Oxygen) < 1)
		{
			SyncOnFire(true, false);
			return;
		}

		ApplyDamage(BURNING_DAMAGE, AttackType.Fire, DamageType.Burn);

		node?.GasMix.AddGas(Gas.Smoke, BURNING_DAMAGE * 100);
	}

	private void SyncOnFire(bool wasOnFire, bool onFire)
	{
		EnsureInit();
		//do nothing if this can't burn
		if (!Resistances.Flammable) return;

		this.onFire = onFire;
		if (this.onFire)
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Add(PeriodicUpdateBurn, BURN_RATE);
			}

			burningObjectOverlay.Burn();
		}
		else
		{
			if (CustomNetworkManager.IsServer)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdateBurn);
			}

			burningObjectOverlay.StopBurning();
		}
	}

	[Server]
	private void CheckDestruction(bool explodeOnDestroy = false)
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

			if (explodeOnDestroy)
			{
				Explosion.StartExplosion(registerTile.WorldPositionServer, ExplosionsDamage);
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
	public void ForceDestroy()
	{
		integrity = 0;
		CheckDestruction();
	}

	public string Examine(Vector3 worldPos)
	{
		string str = "";
		if (integrity < 0.2f * initialIntegrity)
		{
			str = "it's falling apart";
		}
		else if (integrity < 0.4f * initialIntegrity)
		{
			str = "It appears very badly damaged.";
		}
		else if (integrity < 0.6f * initialIntegrity)
		{
			str = "It appears substantially damaged.";
		}
		else if (integrity < 0.8f * initialIntegrity)
		{
			str = "It appears damaged.";
		}
		return str;
	}

	[Server]
	private void DefaultBurnUp(DestructionInfo info)
	{
		Profiler.BeginSample("DefaultBurnUp");
		registerTile.TileChangeManager.MetaTileMap.AddOverlay(registerTile.LocalPosition, isLarge ? LARGE_ASH : SMALL_ASH);
		Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " burnt to ash.", gameObject);
		Loggy.LogTraceFormat("{0} burning up, onfire is {1} (burningObject enabled {2})", Category.Health, name, this.onFire, burningObjectOverlay?.enabled);
		_ = Despawn.ServerSingle(gameObject);
		Profiler.EndSample();
	}

	[Server]
	private void DefaultDestroy(DestructionInfo info)
	{
		if (info.DamageType == DamageType.Brute)
		{
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " got smashed to pieces.", gameObject);
			_ = Despawn.ServerSingle(gameObject);
		}
		//TODO: Other damage types (acid)
		else
		{
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " got destroyed.", gameObject);
			_ = Despawn.ServerSingle(gameObject);
		}
	}

	[Server]
	public void OnExposed(FireExposure exposure)
	{
		Profiler.BeginSample("IntegrityExpose");
		if (exposure.Temperature > HeatResistance)
		{
			ApplyDamage(exposure.StandardDamage(), AttackType.Fire, DamageType.Burn);
		}
		OnExposedEvent.Invoke();
		Profiler.EndSample();
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) ||
		    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold) == false)
		{
			return null;
		}

		return RightClickableResult.Create()
			.AddAdminElement("Smash", AdminSmash)
			.AddAdminElement("Delete", AdminDelete)
			.AddAdminElement("Hotspot", AdminMakeHotspot);
	}

	[NaughtyAttributes.Button()]
	private void AdminSmash()
	{
		AdminCommandsManager.Instance.CmdAdminSmash(gameObject);
	}

	[NaughtyAttributes.Button()]
	private void AdminDelete()
	{
		DevDestroyMessage.Send(gameObject);
	}

	private void AdminMakeHotspot()
	{
		AdminCommandsManager.Instance.CmdAdminMakeHotspot(gameObject);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		OnServerDespawnEvent?.Invoke();
	}

	private void OnDestroy()
	{
		BeingDestroyed?.Invoke();
		BeingDestroyed = null;
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
public class DestructionEvent : UnityEvent<DestructionInfo> { }
public class DamagedEvent : UnityEvent<DamageInfo> { }

/// <summary>
/// Event fired when ApplyDamage is called
/// </summary>
public class DamageInfo
{
	public readonly DamageType DamageType;

	public readonly AttackType AttackType;

	public readonly Integrity AttackedIntegrity;

	public readonly float Damage;
	public DamageInfo(float damage, AttackType attackType, DamageType damageType, Integrity attackedIntegrity)
	{
		DamageType = damageType;
		Damage = damage;
		AttackType = attackType;
		AttackedIntegrity = attackedIntegrity;
	}
}
