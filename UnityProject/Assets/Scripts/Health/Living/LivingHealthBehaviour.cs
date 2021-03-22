﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Atmospherics;
using Light2D;
using Messages.Server.HealthMessages;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using UnityEngine.Profiling;
using WebSocketSharp;

/// <summary>
/// The Required component for all living creatures
/// Monitors and calculates health
/// </summary>
[RequireComponent(typeof(HealthStateMonitor))]
public abstract class LivingHealthBehaviour : NetworkBehaviour, IHealth, IFireExposable, IExaminable, IServerSpawn
{
	private static readonly float GIB_THRESHOLD = 200f;

	//damage incurred per tick per fire stack
	private static readonly float DAMAGE_PER_FIRE_STACK = 0.08f;

	/// <summary>
	/// Invoked when conscious state changes. Provides old state and new state as 1st and 2nd args.
	/// </summary>
	[NonSerialized] public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();

	/// <summary>
	/// Server side, each mob has a different one and never it never changes
	/// </summary>
	public int mobID { get; private set; }

	public float maxHealth = 100;

	public float Resistance { get; } = 50;

	[Tooltip("For mobs that can breath in any atmos environment")]
	public bool canBreathAnywhere = false;

	public float OverallHealth { get; private set; } = 100;
	public float cloningDamage;

	/// <summary>
	/// Serverside, used for gibbing bodies after certain amount of damage is received after death
	/// </summary>
	private float afterDeathDamage = 0f;

	// Systems can also be added via inspector
	public BloodSystem bloodSystem;
	public BrainSystem brainSystem;
	public RespiratorySystem respiratorySystem;

	public BloodSplatType bloodColor;

	/*
	 *  Quick and dirty way to make these hardcoded values dynamic cause different max health values!
	 */

	public int SOFTCRIT_THRESHOLD => 0;
	public int CRIT_THRESHOLD => (int) (0 - maxHealth * 30 / 100);
	public int DEATH_THRESHOLD => (int) -maxHealth;
	public int O2_PASSOUT_THRESHOLD => (int) (maxHealth / 2);


	/// <summary>
	/// If there are any body parts for this living thing, then add them to this list
	/// via the inspector. There needs to be at least 1 chest bodypart for a living animal
	/// </summary>
	[Header("Fill BodyPart fields in via Inspector:")]
	public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

	//For meat harvest (pete etc)
	public bool allowKnifeHarvest;

	[Header("For harvestable animals")] public GameObject[] butcherResults;

	protected DamageType LastDamageType;

	protected GameObject LastDamagedBy;

	public event Action<GameObject> applyDamageEvent;

	public event Action OnDeathNotifyEvent;

	public float RTT;

	public ConsciousState ConsciousState
	{
		get => consciousState;
		protected set
		{
			ConsciousState oldState = consciousState;
			if (value != oldState)
			{
				consciousState = value;
				if (isServer)
				{
					OnConsciousStateChangeServer.Invoke(oldState, value);
				}
			}
		}
	}

	// JSON string for blood types and DNA.
	[SyncVar(hook = nameof(DNASync))] //May remove this in the future and only provide DNA info on request
	private string DNABloodTypeJSON;

	//how on fire we are, sames as tg fire_stacks. 0 = not on fire.
	//It's called "stacks" but it's really just a floating point value that
	//can go up or down based on possible sources of being on fire. Max seems to be 20 in tg.
	[SyncVar(hook = nameof(SyncFireStacks))]
	private float fireStacks;
	private float maxFireStacks = 5f;
	private bool maxFireStacksReached = false;

	/// <summary>
	/// How on fire we are. Exists client side - synced with server.
	/// </summary>
	public float FireStacks => fireStacks;

	/// <summary>
	/// Client side event which fires when this object's fire status changes
	/// (becoming on fire, extinguishing, etc...). Use this to update
	/// burning sprites.
	/// </summary>
	[NonSerialized] public FireStackEvent OnClientFireStacksChange = new FireStackEvent();

	// BloodType and DNA Data.
	private DNAandBloodType DNABloodType;
	private float tickRate = 1f;
	private RegisterTile registerTile;
	private ConsciousState consciousState;

	public bool IsCrit => consciousState == ConsciousState.UNCONSCIOUS;
	public bool IsSoftCrit => consciousState == ConsciousState.BARELY_CONSCIOUS;

	public bool IsDead => consciousState == ConsciousState.DEAD;

	public bool IsSSD => consciousState != ConsciousState.DEAD &&
	                     this is PlayerHealth &&
	                     TryGetComponent(out PlayerScript player) &&
	                     (player.mind == null || player.mind.IsOnline() == false);


	public bool IsBrainDead => consciousState == ConsciousState.DEAD &&
	                           this is PlayerHealth &&
	                           TryGetComponent(out PlayerScript player) &&
	                           (player.mind == null || player.mind.IsOnline() == false);

	/// <summary>
	/// Has the heart stopped.
	/// </summary>
	public bool IsCardiacArrest => bloodSystem.HeartStopped;

	private int damageEffectAttempts = 0;
	private int maxDamageEffectAttempts = 1;


	/// ---------------------------
	/// INIT METHODS
	/// ---------------------------
	public virtual void Awake()
	{
		EnsureInit();
	}

	void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(ServerPeriodicUpdate, tickRate);
		}

		UpdateManager.Add(PeriodicUpdate, 1f);
	}

	void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
		}

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
	}

	/// Add any missing systems:
	public void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
		//Always include blood for living entities:
		bloodSystem = GetComponent<BloodSystem>();
		if (bloodSystem == null)
		{
			bloodSystem = gameObject.AddComponent<BloodSystem>();
		}

		//Always include respiratory for living entities:
		respiratorySystem = GetComponent<RespiratorySystem>();
		if (respiratorySystem == null)
		{
			respiratorySystem = gameObject.AddComponent<RespiratorySystem>();
		}

		respiratorySystem.CanBreatheAnywhere = canBreathAnywhere;

		var tryGetHead = FindBodyPart(BodyPartType.Head);
		if (tryGetHead != null && brainSystem == null)
		{
			if (tryGetHead.Type != BodyPartType.Chest)
			{
				//Head exists, install a brain system
				brainSystem = gameObject.AddComponent<BrainSystem>();
			}
		}
	}

	public override void OnStartServer()
	{
		EnsureInit();
		mobID = PlayerManager.Instance.GetMobID();
		ResetBodyParts();
		if (maxHealth <= 0)
		{
			Logger.LogWarning($"Max health ({maxHealth}) set to zero/below zero!", Category.Health);
			maxHealth = 1;
		}

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
		DNABloodType.BloodColor = bloodColor;
		DNABloodTypeJSON = JsonUtility.ToJson(DNABloodType);
		bloodSystem.SetBloodType(DNABloodType);
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		EnsureInit();
		StartCoroutine(WaitForClientLoad());
	}

	IEnumerator WaitForClientLoad()
	{
		//wait for DNA:
		while (string.IsNullOrEmpty(DNABloodTypeJSON))
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		DNASync(DNABloodTypeJSON, DNABloodTypeJSON);
		SyncFireStacks(fireStacks, this.fireStacks);
	}

	// This is the DNA SyncVar hook
	private void DNASync(string oldDNA, string updatedDNA)
	{
		EnsureInit();
		DNABloodTypeJSON = updatedDNA;
		DNABloodType = JsonUtility.FromJson<DNAandBloodType>(updatedDNA);
	}

	public void Extinguish()
	{
		SyncFireStacks(fireStacks, 0);
	}

	public void ChangeFireStacks(float deltaValue)
	{
		SyncFireStacks(fireStacks, fireStacks + deltaValue);
	}

	private void SyncFireStacks(float oldValue, float newValue)
	{
		EnsureInit();
		this.fireStacks = Math.Max(0, newValue);
		OnClientFireStacksChange.Invoke(this.fireStacks);
	}

	/// <summary>
	/// Check if target body part can take damage, if cannot then replace it
	/// e.x. RightHand -> RightArm
	/// </summary>
	private BodyPartType GetDamageableBodyPart(BodyPartType bodyPartType)
	{
		if (bodyPartType == BodyPartType.Eyes || bodyPartType == BodyPartType.Mouth)
			bodyPartType = BodyPartType.Head;
		else if(bodyPartType == BodyPartType.LeftHand)
			bodyPartType = BodyPartType.LeftArm;
		else if(bodyPartType == BodyPartType.RightHand)
			bodyPartType = BodyPartType.RightArm;
		else if(bodyPartType == BodyPartType.LeftFoot)
			bodyPartType = BodyPartType.LeftLeg;
		else if(bodyPartType == BodyPartType.RightFoot)
			bodyPartType = BodyPartType.RightLeg;

		return bodyPartType;
	}

	/// ---------------------------
	/// PUBLIC FUNCTIONS: HEAL AND DAMAGE:
	/// ---------------------------
	private BodyPartBehaviour GetBodyPart(float amount, DamageType damageType,
		BodyPartType bodyPartAim = BodyPartType.Chest)
	{

		if (amount <= 0 || IsDead)
		{
			return null;
		}

		bodyPartAim = GetDamageableBodyPart(bodyPartAim);

		if (BodyParts.Count == 0)
		{
			Logger.LogError($"There are no body parts to apply a health change to for {gameObject.name}",
				Category.Health);
			return null;
		}

		//See if damage affects the state of the blood:
		// See if any of the healing applied affects blood state
		bloodSystem.AffectBloodState(bodyPartAim, damageType, amount);

		if (damageType == DamageType.Brute || damageType == DamageType.Burn)
		{
			BodyPartBehaviour bodyPartBehaviour = null;

			for (int i = 0; i < BodyParts.Count; i++)
			{
				if (BodyParts[i].Type == bodyPartAim)
				{
					bodyPartBehaviour = BodyParts[i];
					break;
				}
			}

			//If the body part does not exist then try to find the chest instead
			if (bodyPartBehaviour == null)
			{
				var getChestIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Chest);
				if (getChestIndex != -1)
				{
					bodyPartBehaviour = BodyParts[getChestIndex];
				}
				else
				{
					//If there is no default chest body part then do nothing
					Logger.LogError($"No chest body part found for {gameObject.name}", Category.Health);
					return null;
				}
			}

			return bodyPartBehaviour;
		}

		return null;
	}

	/// <summary>
	///  Apply Damage to the whole body of this Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount. will be distributed evenly across all bodyparts</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamage(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{

		foreach (var bodyPart in BodyParts)
		{
			ApplyDamageToBodyPart(damagedBy, damage / BodyParts.Count, attackType, damageType, bodyPart.Type);
		}
	}

	/// <summary>
	///  Apply Damage to random bodypart of the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamageToBodyPart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{
		ApplyDamageToBodyPart(damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize(0));
	}

	/// <summary>
	///  Apply Damage to the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	/// <param name="bodyPartAim">Body Part that is affected</param>
	[Server]
	public virtual void ApplyDamageToBodyPart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType, BodyPartType bodyPartAim)
	{
		TryGibbing(damage);

		var bodyPartBehaviour = GetBodyPart(damage, damageType, bodyPartAim);
		if (bodyPartBehaviour == null)
		{
			return;
		}

		var prevHealth = OverallHealth;

		applyDamageEvent?.Invoke(damagedBy);

		LastDamageType = damageType;
		LastDamagedBy = damagedBy;
		bodyPartBehaviour.ReceiveDamage(damageType, bodyPartBehaviour.armor.GetDamage(damage, attackType));
		HealthBodyPartMessage.Send(gameObject, gameObject, bodyPartAim, bodyPartBehaviour.BruteDamage,
			bodyPartBehaviour.BurnDamage);

		if (attackType == AttackType.Fire)
		{
			// fire stacks should not exceed 20, and not apply if already at the cap
			if (fireStacks <= maxFireStacks && !maxFireStacksReached)
			{
				SyncFireStacks(fireStacks, fireStacks+1);
			}

			// fire stacks should not exceed max fire stacks
			if (fireStacks >= maxFireStacks)
			{
				maxFireStacksReached = true;
			}

			// fire stacks hit 0 remove flag
			if (fireStacks <= 0f)
			{
				maxFireStacksReached = false;
			}
		}

		//For special effects spawning like blood:
		DetermineDamageEffects(damageType);

		Logger.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			damage, prevHealth, OverallHealth, gameObject.name, damageType, bodyPartAim, damagedBy);
	}

	private void TryGibbing(float damage)
	{
		if (!IsDead)
		{
			return;
		}

		afterDeathDamage += damage;

		// if damage IS OVER NINE THOUSAND!!!11!!!1 it means it is coming from a shuttle collision.
		if (damage > 9000f && GameManager.Instance.ShuttleGibbingAllowed)
		{
			Harvest();
			return;
		}

		if (!GameManager.Instance.GibbingAllowed)
		{
			return;
		}

		if (afterDeathDamage >= GIB_THRESHOLD)
		{
			Harvest();
		}
	}

	/// <summary>
	///  Apply healing to a living thing. Server Only
	/// </summary>
	/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
	/// <param name="healAmt">Amount of healing to add</param>
	/// <param name="damageType">The Type of Damage To Heal</param>
	/// <param name="bodyPartAim">Body Part to heal</param>
	[Server]
	public virtual void HealDamage(GameObject healingItem, int healAmt,
		DamageType damageTypeToHeal, BodyPartType bodyPartAim)
	{
		BodyPartBehaviour bodyPartBehaviour = GetBodyPart(healAmt, damageTypeToHeal, bodyPartAim);
		if (bodyPartBehaviour == null)
		{
			return;
		}

		bodyPartBehaviour.HealDamage(healAmt, damageTypeToHeal);
		HealthBodyPartMessage.Send(gameObject, gameObject, bodyPartAim, bodyPartBehaviour.BruteDamage,
			bodyPartBehaviour.BurnDamage);

		var prevHealth = OverallHealth;
		Logger.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
			healAmt, prevHealth, OverallHealth, gameObject.name, damageTypeToHeal, bodyPartAim, healingItem);
	}


	public void OnExposed(FireExposure exposure)
	{
		Profiler.BeginSample("PlayerExpose");
		ApplyDamage(null, 1, AttackType.Fire, DamageType.Burn);
		Profiler.EndSample();
	}

	/// ---------------------------
	/// UPDATE LOOP
	/// ---------------------------

	//Handled via UpdateManager
	void ServerPeriodicUpdate()
	{
		// TODO If becomes dead, why not remove from UpdateManager?
		if (IsDead) return;

		if (fireStacks > 0)
		{
			//TODO: Burn clothes (see species.dm handle_fire)
			ApplyDamageToBodyPart(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Fire, DamageType.Burn);
			//gradually deplete fire stacks
			SyncFireStacks(fireStacks, fireStacks - 0.1f);
			//instantly stop burning if there's no oxygen at this location
			MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionClient);
			if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
			{
				SyncFireStacks(fireStacks, 0);
			}

			registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition());
		}

		CalculateRadiationDamage();
		CalculateOverallHealth();
		CheckHealthAndUpdateConsciousState();
	}

	private void PeriodicUpdate()
	{
		if (damageEffectAttempts >= maxDamageEffectAttempts)
		{
			damageEffectAttempts = 0;
		}
	}


	public float RadiationStacks = 0;

	/// <summary>
	/// Radiation damage Calculations
	/// </summary>
	[Server]
	public void CalculateRadiationDamage()
	{
		var RadLevel = (registerTile.Matrix.GetRadiationLevel(registerTile.LocalPosition) * (tickRate / 5f) / 6);
		var Chest = BodyParts.First(part => part.Type == BodyPartType.Chest);
		RadiationStacks += Chest.armor.GetDamage(RadLevel, AttackType.Rad);

		var ProcessingRadiation = RadiationStacks * 0.001f;
		if (ProcessingRadiation < 20 && ProcessingRadiation > 0.5f)
		{
			ProcessingRadiation = 20;
		}

		RadiationStacks -= ProcessingRadiation;
		bloodSystem.ToxinLevel +=  ProcessingRadiation * 0.05f;

		//Natural healing
		//Problems should be in the metabolic system
		//but thats on players only
		bloodSystem.ToxinLevel -= 0.01f;

		if (RadiationStacks < 0)
		{
			RadiationStacks = 0;
		}
	}

	/// ---------------------------
	/// VISUAL EFFECTS
	/// ---------------------------
	/// <Summary>
	/// Used to determine any special effects spawning cased by a damage type
	/// Server only
	/// </Summary>
	[Server]
	protected virtual void DetermineDamageEffects(DamageType damageType)
	{
		if (damageEffectAttempts >= maxDamageEffectAttempts)
		{
			return;
		}

		damageEffectAttempts++;

		//Brute attacks
		if (damageType == DamageType.Brute)
		{
			//spawn blood
			EffectsFactory.BloodSplat(registerTile.WorldPositionServer, BloodSplatSize.medium, bloodColor);
		}
	}

	/// ---------------------------
	/// HEALTH CALCULATIONS
	/// ---------------------------
	/// <summary>
	/// Recalculates the overall player health and updates OverallHealth property. Server only
	/// </summary>
	[Server]
	public void CalculateOverallHealth()
	{
		float newHealth = maxHealth;
		newHealth -= CalculateOverallBodyPartDamage();
		newHealth -= CalculateOverallBloodLossDamage();
		newHealth -= bloodSystem.OxygenDamage;
		newHealth -= cloningDamage;
		OverallHealth = newHealth;
	}

	public float CalculateOverallBodyPartDamage()
	{
		float bodyPartDmg = 0;
		for (int i = 0; i < BodyParts.Count; i++)
		{
			bodyPartDmg += BodyParts[i].BruteDamage;
			bodyPartDmg += BodyParts[i].BurnDamage;
		}

		return bodyPartDmg;
	}

	public float GetTotalBruteDamage()
	{
		float bruteDmg = 0;
		for (int i = 0; i < BodyParts.Count; i++)
		{
			bruteDmg += BodyParts[i].BruteDamage;
		}

		return bruteDmg;
	}

	public float GetTotalBurnDamage()
	{
		float burnDmg = 0;
		for (int i = 0; i < BodyParts.Count; i++)
		{
			burnDmg += BodyParts[i].BurnDamage;
		}

		return burnDmg;
	}

	/// Blood Loss and Toxin damage:
	public int CalculateOverallBloodLossDamage()
	{
		float maxBloodDmg = Mathf.Abs(DEATH_THRESHOLD) + maxHealth;
		float bloodDmg = 0f;
		if (bloodSystem.BloodLevel < (int) BloodVolume.SAFE)
		{
			bloodDmg = Mathf.Lerp(0f, maxBloodDmg, 1f - (bloodSystem.BloodLevel / (float) BloodVolume.NORMAL));
		}


		if (bloodSystem.ToxinLevel > 1f)
		{
			//TODO determine a way to handle toxin damage when toxins are implemented
			//There will need to be some kind of blood / toxin ratio and severity limits determined
		}

		//to Whoever put this here /\, am Just make it simple
		bloodDmg += bloodSystem.ToxinLevel;

		return Mathf.RoundToInt(Mathf.Clamp(bloodDmg, 0f, maxBloodDmg));
	}

	/// ---------------------------
	/// CRIT + DEATH METHODS
	/// ---------------------------
	///Death from other causes
	public virtual void Death()
	{
		if (IsDead)
		{
			return;
		}

		OnDeathNotifyEvent?.Invoke();
		afterDeathDamage = 0;
		ConsciousState = ConsciousState.DEAD;
		OnDeathActions();
		bloodSystem.StopBleedingAll();
		//stop burning
		//TODO: When clothes/limb burning is implemented, probably should keep burning until clothes are burned up
		SyncFireStacks(fireStacks, 0);
	}

	private void Crit(bool allowCrawl = false)
	{
		var proposedState = allowCrawl ? ConsciousState.BARELY_CONSCIOUS : ConsciousState.UNCONSCIOUS;

		if (ConsciousState == proposedState || IsDead)
		{
			return;
		}

		ConsciousState = proposedState;
	}

	private void Uncrit()
	{
		var proposedState = ConsciousState.CONSCIOUS;
		if (ConsciousState == proposedState || IsDead)
		{
			return;
		}

		ConsciousState = proposedState;
	}

	/// <summary>
	/// Checks if the player's health has changed such that consciousstate needs to be changed,
	/// and changes consciousstate and invokes whatever needs to be invoked when the state changes
	/// </summary>
	private void CheckHealthAndUpdateConsciousState()
	{
		if (ConsciousState != ConsciousState.CONSCIOUS && bloodSystem.OxygenDamage < O2_PASSOUT_THRESHOLD &&
		    OverallHealth > SOFTCRIT_THRESHOLD)
		{
			Logger.LogFormat("{0}, back on your feet!", Category.Health, gameObject.name);
			Uncrit();
			return;
		}

		if (OverallHealth <= SOFTCRIT_THRESHOLD || bloodSystem.OxygenDamage > O2_PASSOUT_THRESHOLD)
		{
			if (OverallHealth <= CRIT_THRESHOLD)
			{
				Crit(false);
			}
			else
			{
				Crit(true); //health isn't low enough for crit, but might be low enough for soft crit or passed out from lack of oxygen
			}
		}

		if (NotSuitableForDeath())
		{
			return;
		}

		Death();
	}

	private bool NotSuitableForDeath()
	{
		return OverallHealth > DEATH_THRESHOLD || IsDead;
	}

	protected abstract void OnDeathActions();

	// --------------------
	// UPDATES FROM SERVER
	// --------------------

	// Stats are separated so that the server only updates the area of concern when needed

	/// <summary>
	/// Updates the main health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientHealthStats(float overallHealth)
	{
		OverallHealth = overallHealth;
		//	Logger.Log($"Update stats for {gameObject.name} OverallHealth: {overallHealth} ConsciousState: {consciousState.ToString()}", Category.Health);
	}

	/// <summary>
	/// Updates the conscious state from the server via NetMsg
	/// </summary>
	public void UpdateClientConsciousState(ConsciousState proposedState)
	{
		ConsciousState = proposedState;
	}

	/// <summary>
	/// Updates the respiratory health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientRespiratoryStats(bool value)
	{
		respiratorySystem.IsSuffocating = value;
	}

	public void UpdateClientTemperatureStats(float value)
	{
		respiratorySystem.temperature = value;
	}

	public void UpdateClientPressureStats(float value)
	{
		respiratorySystem.pressure = value;
	}

	/// <summary>
	/// Updates the blood health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBloodStats(int heartRate, float bloodVolume, float oxygenDamage, float toxinLevel)
	{
		bloodSystem.UpdateClientBloodStats(heartRate, bloodVolume, oxygenDamage, toxinLevel);
	}

	/// <summary>
	/// Updates the brain health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBrainStats(bool isHusk, int brainDamage)
	{
		if (brainSystem != null)
		{
			brainSystem.UpdateClientBrainStats(isHusk, brainDamage);
		}
	}

	/// <summary>
	/// Updates the bodypart health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBodyPartStats(BodyPartType bodyPartType, float bruteDamage, float burnDamage)
	{
		var bodyPart = FindBodyPart(bodyPartType);
		if (bodyPart != null)
		{
			//	Logger.Log($"Update stats for {gameObject.name} body part {bodyPartType.ToString()} BruteDmg: {bruteDamage} BurnDamage: {burnDamage}", Category.Health);

			bodyPart.UpdateClientBodyPartStat(bruteDamage, burnDamage);
		}
	}

	/// ---------------------------
	/// Electrocution Methods
	/// ---------------------------
	/// Note: Electrocution for players is extended in PlayerHealth deriviative.
	/// This is a generic electrocution implementation that just deals damage.
	/// <summary>
	/// Electrocutes a mob, applying damage to the victim depending on the electrocution power.
	/// </summary>
	/// <param name="electrocution">The object containing all information for this electrocution</param>
	/// <returns>Returns an ElectrocutionSeverity for when the following logic depends on the elctrocution severity.</returns>
	public virtual LivingShockResponse Electrocute(Electrocution electrocution)
	{
		float resistance = ApproximateElectricalResistance(electrocution.Voltage);
		float shockPower = Electrocution.CalculateShockPower(electrocution.Voltage, resistance);
		var severity = GetElectrocutionSeverity(shockPower);

		switch (severity)
		{
			case LivingShockResponse.None:
				break;
			case LivingShockResponse.Mild:
				MildElectrocution(electrocution, shockPower);
				break;
			case LivingShockResponse.Painful:
				PainfulElectrocution(electrocution, shockPower);
				break;
			case LivingShockResponse.Lethal:
				LethalElectrocution(electrocution, shockPower);
				break;
		}

		return severity;
	}

	/// <summary>
	/// Finds the severity of the electrocution.
	/// In the future, this would depend on the victim's size. For now, assume humanoid size.
	/// </summary>
	/// <param name="shockPower">The power of the electrocution determines the shock response </param>
	protected virtual LivingShockResponse GetElectrocutionSeverity(float shockPower)
	{
		LivingShockResponse severity;

		if (shockPower >= 0.01 && shockPower < 1) severity = LivingShockResponse.Mild;
		else if (shockPower >= 1 && shockPower < 100) severity = LivingShockResponse.Painful;
		else if (shockPower >= 100) severity = LivingShockResponse.Lethal;
		else severity = LivingShockResponse.None;

		return severity;
	}

	// Overrideable for custom electrical resistance calculations.
	protected virtual float ApproximateElectricalResistance(float voltage)
	{
		// TODO: Approximate mob's electrical resistance based on mob size.
		return 500;
	}

	protected virtual void MildElectrocution(Electrocution electrocution, float shockPower)
	{
		return;
	}

	protected virtual void PainfulElectrocution(Electrocution electrocution, float shockPower)
	{
		LethalElectrocution(electrocution, shockPower);
	}

	protected virtual void LethalElectrocution(Electrocution electrocution, float shockPower)
	{
		// TODO: Add sparks VFX at shockSourcePos.
		SoundManager.PlayNetworkedAtPos(SingletonSOSounds.Instance.Sparks, electrocution.ShockSourcePos);

		float damage = shockPower;
		ApplyDamage(null, damage, AttackType.Internal, DamageType.Burn);
		if (gameObject.TryGetComponent<PlayerEffectsManager>(out var effectsManager) == false)
		{
			return;
		}

		effectsManager.ShakePlayer(6f, 0.1f, 0.1f);
	}

	/// ---------------------------
	/// MISC Functions:
	/// ---------------------------
	///<summary>
	/// If Harvesting is allowed (for pete the goat for example)
	/// then spawn the butchered results
	/// </summary>
	[Server]
	public void Harvest()
	{
		foreach (GameObject harvestPrefab in butcherResults)
		{
			Spawn.ServerPrefab(harvestPrefab, transform.position, parent: transform.parent);
		}

		Gib();
	}

	[Server]
	protected virtual void Gib()
	{
		EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
		//todo: actual gibs

		//never destroy players!
		Despawn.ServerSingle(gameObject);
	}

	public BodyPartBehaviour FindBodyPart(BodyPartType bodyPartAim)
	{
		int searchIndex = BodyParts.FindIndex(x => x.Type == bodyPartAim);
		if (searchIndex != -1)
		{
			return BodyParts[searchIndex];
		}

		bodyPartAim = GetDamageableBodyPart(bodyPartAim);

		//If nothing is found then try to find a chest component:
		searchIndex = BodyParts.FindIndex(x => x.Type == BodyPartType.Chest);
		if (searchIndex != -1)
		{
			return BodyParts[searchIndex];
		}

		// else nothing:
		return null;
	}

	/// <summary>
	/// Reset all body part damage.
	/// </summary>
	[Server]
	private void ResetBodyParts()
	{
		foreach (BodyPartBehaviour bodyPart in BodyParts)
		{
			bodyPart.RestoreDamage();
			bodyPart.livingHealthBehaviour = this;
		}
	}

	private void OnDrawGizmos()
	{
		if (!Application.isPlaying)
		{
			return;
		}

		Gizmos.color = Color.blue.WithAlpha(0.5f);
		Gizmos.DrawCube(registerTile.WorldPositionServer, Vector3.one);
	}

	/// <summary>
	/// This is just a simple initial implementation of IExaminable to health;
	/// can potentially be extended to return more details and let the server
	/// figure out what to pass to the client, based on many parameters such as
	/// role, medical skill (if they get implemented), equipped medical scanners,
	/// etc. In principle takes care of building the string from start to finish,
	/// so logic generating examine text can be completely separate from examine
	/// request or netmessage processing.
	/// </summary>
	public string Examine(Vector3 worldPos)
	{
		if (this is PlayerHealth)
		{
			// Let ExaminablePlayer take care of this.
			return default;
		}

		return GetExamineText();
	}

	public string GetExamineText()
	{
		// Assume animal
		string theyPronoun = "It";
		string theirPronoun = "its";

		var ps = GetComponentInParent<PlayerScript>();
		var cs = ps?.characterSettings;
		if (cs != null)
		{
			theyPronoun = cs.TheyPronoun(ps).Capitalize();
			theirPronoun = cs.TheirPronoun(ps);
		}

		var healthString = $"{theyPronoun} is ";
		if (IsDead)
		{
			healthString += "limp and unresponsive; there are no signs of life";
			if (IsSSD)
			{
				healthString += $" and {theirPronoun} soul has departed";
			}

			healthString += "...";
		}
		else // Is alive
		{
			healthString += $"{ConsciousState.ToString().ToLower().Replace("_", " ")} and ";

			var healthFraction = OverallHealth / maxHealth;
			string healthDescription;
			if (healthFraction < 0.2f)
			{
				healthDescription = "heavily wounded.";
			}
			else if (healthFraction < 0.6f)
			{
				healthDescription = "wounded.";
			}
			else
			{
				healthDescription = "in good shape.";
			}

			if (respiratorySystem.IsSuffocating)
			{
				healthDescription = "having trouble breathing!";
			}
			// On fire?
			if (FireStacks > 0)
			{
				healthDescription = "on fire!";
			}
			healthString += healthDescription;

			if (this is PlayerHealth && GetComponent<PlayerScript>().mind.IsOnline() == false)
			{
				healthString += $"\n{theyPronoun} has a blank, absent-minded stare and appears completely unresponsive to anything. " +
				                $"{theyPronoun} may snap out of it soon.";
			}
		}

		return healthString;
	}

	public string GetShortStatus()
	{
		if (IsBrainDead)
		{
			return "BRAINDEAD";
		}
		if (IsDead)
		{
			return "DEAD";
		}

		if (IsSSD)
		{
			return "SSD";
		}

		if (IsCrit || IsSoftCrit)
		{
			return "CRITICAL";
		}

		return "OK";
	}

	public string GetWoundsDescription()
	{
		var description = new StringBuilder();

		foreach (var part in BodyParts)
		{
			if (part.GetDamageDescription().IsNullOrEmpty())
			{
				continue;
			}
			description.AppendFormat("\n<b>{0}</b> is {1}.\n", part.Type.ToString(), part.GetDamageDescription());
		}

		return description.ToString();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ConsciousState = ConsciousState.CONSCIOUS;
		OverallHealth = maxHealth;
		ResetBodyParts();
		CalculateOverallHealth();
	}
}

/// <summary>
/// Event which fires when fire stack value changes.
/// </summary>
public class FireStackEvent : UnityEvent<float>
{
}

/// <summary>
/// Communicates fire status changes.
/// </summary>
public class FireStatus
{
	//whether becoming on fire or extinguished
	public readonly bool IsOnFire;

	//whether we are engulfed by flames or just partially on fire
	public readonly bool IsEngulfed;

	public FireStatus(bool isOnFire, bool isEngulfed)
	{
		IsOnFire = isOnFire;
		IsEngulfed = isEngulfed;
	}
}

// Commented out this code as they were hardcoded values. I'm leaving them here for future reference on what
// were the intended values considering a max health of 100 that can go to -100.
// public static class HealthThreshold
// {
// 	public const int SoftCrit = 0;
// 	public const int Crit = -30;
// 	public const int Dead = -100;
// 	public const int OxygenPassOut = 50;
// }

/// <summary>
/// Event which fires when conscious state changes, provides the old state and the new state
/// </summary>
public class ConsciousStateEvent : UnityEvent<ConsciousState, ConsciousState>
{
}
