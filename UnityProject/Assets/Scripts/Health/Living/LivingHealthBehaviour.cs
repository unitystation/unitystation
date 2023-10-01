using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Mirror;
using Messages.Server.HealthMessages;
using Systems.Atmospherics;
using Light2D;
using HealthV2;
using Logs;
using Newtonsoft.Json;


/// <summary>
/// The Required component for all living creatures
/// Monitors and calculates health
/// </summary>
[Obsolete("LivingHealthBehaviour is deprecated, please use LivingHealthMasterBase instead unless you are working on V1 Mobs.")]
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

	private DateTime timeOfDeath;
	private DateTime TimeOfDeath => timeOfDeath;

	public float OverallHealth { get; private set; } = 100;
	[NonSerialized]
	public float cloningDamage;

	/// <summary>
	/// Serverside, used for gibbing bodies after certain amount of damage is received after death
	/// </summary>
	private float afterDeathDamage = 0f;

	public BloodSplatType bloodColor;

	/*
	 *  Quick and dirty way to make these hardcoded values dynamic cause different max health values!
	 */

	public int SOFTCRIT_THRESHOLD => 0;
	public int CRIT_THRESHOLD => (int) (0 - maxHealth * 30 / 100);
	public int DEATH_THRESHOLD => (int) -maxHealth;

	/// <summary>
	/// If there are any body parts for this living thing, then add them to this list
	/// via the inspector. There needs to be at least 1 chest bodypart for a living animal
	/// </summary>
	[Header("Fill BodyPart fields in via Inspector:")]
	public List<BodyPartBehaviour> BodyParts = new List<BodyPartBehaviour>();

	[Header("For harvestable animals")] public GameObject[] butcherResults;

	public event Action<GameObject> applyDamageEvent;

	public event Action OnDeathNotifyEvent;

	[NonSerialized]
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
	private UniversalObjectPhysics objectBehaviour;
	private ConsciousState consciousState;

	public bool IsCrit => consciousState == ConsciousState.UNCONSCIOUS;
	public bool IsSoftCrit => consciousState == ConsciousState.BARELY_CONSCIOUS;

	public bool IsDead => consciousState == ConsciousState.DEAD;

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
		if(CustomNetworkManager.IsServer == false) return;

		UpdateManager.Add(ServerPeriodicUpdate, tickRate);
	}

	void OnDisable()
	{
		if(CustomNetworkManager.IsServer == false) return;

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, ServerPeriodicUpdate);
	}

	/// Add any missing systems:
	public void EnsureInit()
	{
		if (registerTile != null) return;
		registerTile = GetComponent<RegisterTile>();
		objectBehaviour = GetComponent<UniversalObjectPhysics>();
		//Always include blood for living entities:
	}

	public override void OnStartServer()
	{
		EnsureInit();
		mobID = PlayerManager.Instance.GetMobID();
		ResetBodyParts();
		if (maxHealth <= 0)
		{
			Loggy.LogWarning($"Max health ({maxHealth}) set to zero/below zero!", Category.Health);
			maxHealth = 1;
		}

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
		DNABloodType.BloodColor = bloodColor;
		DNABloodTypeJSON = JsonConvert.SerializeObject(DNABloodType);
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
		DNABloodType = JsonConvert.DeserializeObject<DNAandBloodType>(updatedDNA);
	}

	/// <summary>
	/// Adjusts the amount of fire stacks, to a min of 0 (not on fire) and a max of maxFireStacks
	/// </summary>
	/// <param name="deltaValue">The amount to adjust the stacks by, negative if reducing positive if increasing</param>
	public void ChangeFireStacks(float deltaValue)
	{
		SyncFireStacks(fireStacks, Mathf.Clamp((fireStacks + deltaValue), 0, maxFireStacks));
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
			Loggy.LogError($"There are no body parts to apply a health change to for {gameObject.name}",
				Category.Health);
			return null;
		}

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
					Loggy.LogError($"No chest body part found for {gameObject.name}", Category.Health);
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

		Loggy.LogTraceFormat("{3} received {0} {4} damage from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
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
		Loggy.LogTraceFormat("{3} received {0} {4} healing from {6} aimed for {5}. Health: {1}->{2}", Category.Health,
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
	private void ServerPeriodicUpdate()
	{
		if (damageEffectAttempts >= maxDamageEffectAttempts)
		{
			damageEffectAttempts = 0;
		}

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

			registerTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition(), 700);
		}

		if (IsDead)
		{
			DeathPeriodicUpdate();
			return;
		}

		CalculateOverallHealth();
		CheckHealthAndUpdateConsciousState();
	}



	/// ---------------------------
	/// VISUAL EFFECTS
	/// ---------------------------
	/// <Summary>
	/// Used to determine any special effects spawning cased by a damage type
	/// Server only
	/// </Summary>
	[Server]
	protected void DetermineDamageEffects(DamageType damageType)
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
			//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
			//EffectsFactory.BloodSplat(registerTile.WorldPositionServer, BloodSplatSize.medium, bloodColor);
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

	/// ---------------------------
	/// CRIT + DEATH METHODS
	/// ---------------------------
	///Death from other causes
	[Obsolete("LivingHealthBehaviour is deprecated, please use LivingHealthMasterBase instead unless you are working on V1 Mobs.")]
	public void Death()
	{
		if (IsDead)
		{
			return;
		}

		timeOfDeath = GameManager.Instance.RoundTime;

		OnDeathNotifyEvent?.Invoke();
		afterDeathDamage = 0;
		ConsciousState = ConsciousState.DEAD;
		OnDeathActions();
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
		if (ConsciousState != ConsciousState.CONSCIOUS && OverallHealth > SOFTCRIT_THRESHOLD)
		{
			Loggy.LogFormat("{0}, back on your feet!", Category.Health, gameObject.name);
			Uncrit();
			return;
		}

		if (OverallHealth <= SOFTCRIT_THRESHOLD)
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

	private void DeathPeriodicUpdate()
	{
		MiasmaCreation();
	}

	[Obsolete("LivingHealthBehaviour is deprecated, please use LivingHealthMasterBase instead unless you are working on V1 Mobs.")]
	private void MiasmaCreation()
	{
		//Don't produce miasma until 2 minutes after death
		if (GameManager.Instance.RoundTime.Subtract(timeOfDeath).TotalMinutes < 2) return;

		MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionClient);

		//Space or below -10 degrees celsius is safe from miasma creation
		if (node.IsSpace || node.GasMix.Temperature <= Reactions.KOffsetC - 10) return;

		//If we are in a container then don't produce miasma
		if (objectBehaviour.ContainedInObjectContainer != null) return;

		node.GasMix.AddGas(Gas.Miasma, AtmosDefines.MIASMA_CORPSE_MOLES);
	}

	private bool NotSuitableForDeath()
	{
		return OverallHealth > DEATH_THRESHOLD || IsDead;
	}

	protected abstract void OnDeathActions();

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

	[Obsolete("LivingHealthBehaviour is deprecated, please use LivingHealthMasterBase.OnGib() instead unless you are working on V1 Mobs.")]
	[Server]
	protected virtual void Gib()
	{
		//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
		//EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, bloodColor);
		//todo: actual gibs

		//never destroy players!
		_ = Despawn.ServerSingle(gameObject);
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
		return GetExamineText();
	}

	public string GetExamineText()
	{
		// Assume animal
		string theyPronoun = gameObject.GetTheyPronoun();
		//string theirPronoun = gameObject.GetTheirPronoun()

		var healthString = $"{theyPronoun} is ";
		if (IsDead)
		{
			healthString += "limp and unresponsive; there are no signs of life";


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

			// On fire?
			if (FireStacks > 0)
			{
				healthDescription = "on fire!";
			}
			healthString += healthDescription;


		}

		return healthString;
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ConsciousState = ConsciousState.CONSCIOUS;
		OverallHealth = maxHealth;
		ResetBodyParts();
		CalculateOverallHealth();
	}
}



