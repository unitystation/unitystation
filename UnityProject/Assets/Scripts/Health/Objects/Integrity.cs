using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using Mirror;
using AddressableReferences;
using AdminCommands;
using Logs;
using Messages.Client.DevSpawner;
using Systems.Explosions;
using Systems.Interaction;
using UI.Systems.Tooltips.HoverTooltips;
using Util.Independent.FluentRichText;

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
public class Integrity : NetworkBehaviour, IHealth, IFireExposable, IRightClickable, IServerSpawn, IExaminable, IServerDespawn, IHoverTooltip
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

	public UnityEvent OnDamaged = new UnityEvent();
	public UnityEvent OnDestruction = new UnityEvent();

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

	[field: SyncVar] public float integrity { get; private set; } = 100f;
	private bool destroyed = false;
	private DamageInfo lastDamageInfo;
	private DamageType lastDamageType => lastDamageInfo.DamageType;
	private RegisterTile registerTile;
	public RegisterTile RegisterTile => registerTile;
	private UniversalObjectPhysics universalObjectPhysics;

	private Meleeable meleeable;
	public Meleeable Meleeable => meleeable;

	//The current integrity divided by the initial integrity
	public float PercentageDamaged => integrity.Approx(0) ? 0 : integrity / initialIntegrity * 100f;

	//whether this is a large object (meaning we would use the large ash pile and large burning sprite)
	private bool isLarge;
	public float Resistance => integrity * ((int)universalObjectPhysics.GetSize() / 10f);

	private void Awake()
	{
		EnsureInit();
	}

	private void OnDisable()
	{
		OnDamaged?.RemoveAllListeners();
	}

	private void EnsureInit()
	{
		if (registerTile != null) return;
		meleeable = GetComponent<Meleeable>();
		registerTile = GetComponent<RegisterTile>();
		universalObjectPhysics = GetComponent<UniversalObjectPhysics>();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		if (info.SpawnType == SpawnType.Clone)
		{
			//cloned
			var clonedIntegrity = info.ClonedFrom.GetComponent<Integrity>();
			integrity = clonedIntegrity.integrity;
			destroyed = clonedIntegrity.destroyed;
		}
		else
		{
			//spawned
			integrity = initialIntegrity;
			destroyed = false;
		}
	}

	public override void OnStartClient()
	{
		EnsureInit();
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
			integrity -= damage;
			lastDamageInfo = damageInfo;

			if (triggerEvent)
			{
				OnApplyDamage?.Invoke(damageInfo);
				OnDamaged?.Invoke();
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
		if (node?.GasMixLocal.GetMoles(Gas.Oxygen) < 1)
		{
			SyncOnFire(true, false);
			return;
		}

		ApplyDamage(BURNING_DAMAGE, AttackType.Fire, DamageType.Burn);

		node?.GasMixLocal.AddGasWithTemperature(Gas.Smoke, BURNING_DAMAGE * 100, node.GasMixLocal.Temperature);
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
			OnDestruction?.Invoke();
			var destructInfo = new DestructionInfo(lastDamageType, this);
			OnWillDestroyServer.Invoke(destructInfo);

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
			}
			DefaultDestroy(destructInfo);
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
		return GetDamageDesc().Italic();
	}

	[Server]
	private void DefaultDestroy(DestructionInfo info)
	{
		var destructionText = info.DamageType switch
		{
			DamageType.Brute => " got smashed to pieces.",
			DamageType.Burn => " burns to nothing.",
			DamageType.Tox => " withers away.",
			DamageType.Oxy => " got smashed to pieces.",
			DamageType.Clone => " crumbles onto itself.",
			DamageType.Stamina => " exaughusts itself out of existence.",
			DamageType.Radiation => " atmos delink from each other and crumble.",
			_ => " got smashed to pieces."
		};
		Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), destructionText, gameObject);
		_ = Despawn.ServerSingle(gameObject);
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
			.AddAdminElement("[Debug] - Smash", AdminSmash)
			.AddAdminElement("[Debug] - Delete", AdminDelete)
			.AddAdminElement("[Debug] - Hotspot", AdminMakeHotspot);
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

	public string GetDamageDesc()
	{
		return "It is " + PercentageDamaged switch
		{
			< 10 => "crumbling.".Color(Color.red),
			< 30 => "heavily damaged.".Color(Color.red),
			< 40 => "significantly damaged.".Color(Color.yellow),
			< 60 => "in a " + "worn out condition.".Color(Color.yellow),
			< 80 => "slightly damaged.".Color(Color.green),
			< 95 => "in a " + "scratched condition.".Color(Color.green),
			>= 100 => "in " + "perfect condition.".Color(Color.green),
			_ => "in an unknown condition."
		};
	}

	public string HoverTip()
	{
#if UNITY_EDITOR
		// we don't check this outside the editor because because Mirror can't put DamageInfo in a syncvar. So if we check this during
		// runtime, we'll only a null value. So simply just never compile this for regural players because it wont be used elsewhere.
		if (lastDamageInfo != null && KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold))
		{
			return $"Last Damage Number: {lastDamageInfo.Damage}\n Damage Type: {lastDamageInfo.DamageType}\n Attack Type: {lastDamageInfo.AttackType}]\n Integrity: {integrity}";
		}
#endif
		return GetDamageDesc();
	}

	public string CustomTitle()
	{
		return null;
	}

	public Sprite CustomIcon()
	{
		return null;
	}

	public List<Sprite> IconIndicators()
	{
		return null;
	}

	public List<TextColor> InteractionsStrings()
	{
		return null;
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
	public DamageType DamageType;
	public AttackType AttackType;
	public Integrity AttackedIntegrity;
	public float Damage;

	public DamageInfo(float damage, AttackType attackType, DamageType damageType, Integrity attackedIntegrity)
	{
		DamageType = damageType;
		Damage = damage;
		AttackType = attackType;
		AttackedIntegrity = attackedIntegrity;
	}
}
