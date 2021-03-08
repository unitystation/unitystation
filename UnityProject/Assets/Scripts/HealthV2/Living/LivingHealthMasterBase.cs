using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Systems.Atmospherics;
using Chemistry;
using Health.Sickness;
using HealthV2;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

public abstract class LivingHealthMasterBase : NetworkBehaviour
{
	/// <summary>
	/// Server side, each mob has a different one and never it never changes
	/// </summary>
	public int mobID { get; private set; }

	private DNAandBloodType DNABloodType;

	// JSON string for blood types and DNA.
	[SyncVar(hook = nameof(DNASync))] //May remove this in the future and only provide DNA info on request
	private string DNABloodTypeJSON;

	private float tickRate = 1f;
	private float tick = 0;


	public float AvailableBlood = 0;


	private RegisterTile registerTile;
	public RegisterTile RegisterTile => registerTile;

	[NonSerialized] public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();

	//damage incurred per tick per fire stack
	private static readonly float DAMAGE_PER_FIRE_STACK = 0.08f;

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

				if (value == ConsciousState.DEAD)
				{
					Death();
				}
			}
		}
	}

	public BodyType BodyType = BodyType.Neutral;

	private ConsciousState consciousState;

	//I don't know what this is. It was in the first Health system.
	public float RTT;

	[SerializeField] private float maxHealth = 100; //plyer sytuff

	private float overallHealth = 100;
	public float OverallHealth => overallHealth;

	[SerializeField] [Tooltip("These are the things that will hold all our organs and implants.")]
	private List<BodyPart> bodyPartContainers;

	[CanBeNull] private CirculatorySystemBase circulatorySystem;
	public CirculatorySystemBase CirculatorySystem => circulatorySystem;


	private HealthV2.Brain brain;


	[CanBeNull] private RespiratorySystemBase respiratorySystem;
	public RespiratorySystemBase RespiratorySystem => respiratorySystem;

	[CanBeNull] private MetabolismSystemV2 metabolism;
	//public MetabolismSystemV2 Metabolism => metabolism;

	private bool isDead
	{
		get
		{
			if (ConsciousState.DEAD == ConsciousState)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsDead => isDead;

	public bool IsCrit => ConsciousState == ConsciousState.UNCONSCIOUS;
	public bool IsSoftCrit => ConsciousState == ConsciousState.BARELY_CONSCIOUS;

	private HashSet<BodyPart> implantList = new HashSet<BodyPart>();

	public HashSet<BodyPart> ImplantList => implantList;

	public List<RootBodyPartContainer> RootBodyPartContainers = new List<RootBodyPartContainer>();

	public event Action<GameObject> applyDamageEvent;

	public event Action OnDeathNotifyEvent;

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

	private ObjectBehaviour objectBehaviour;
	public ObjectBehaviour OBehavior => objectBehaviour;


	// public float MaxNutrimentLevel = 30;

	// public float NutrimentLevel = 20;

	public Reagent Cem;

	public HungerState hungerState => CalculateHungerState();

	public HungerState CalculateHungerState()
	{
		//hummm
		// if (MaxNutrimentLevel < NutrimentLevel)
		// {
			// return HungerState.Full;
		// }
		// else if (NutrimentLevel != 0)
		// {
			// return HungerState.Normal;
		// }
		// else if (NutrimentLevel == 0)
		// {
			// return HungerState.Starving;
		// }

		return HungerState.Normal;
	}

	public virtual void Awake()
	{
		EnsureInit();
	}

	void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		UpdateManager.Add(PeriodicUpdate, 1f);
	}

	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
	}

	private void EnsureInit()
	{
		if (registerTile) return;
		registerTile = GetComponent<RegisterTile>();
		respiratorySystem = GetComponent<RespiratorySystemBase>();
		circulatorySystem = GetComponent<CirculatorySystemBase>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
		//Always include blood for living entities:
	}

	public void Setbrain(HealthV2.Brain _brain)
	{
		brain = _brain;
	}

	public override void OnStartServer()
	{
		EnsureInit();
		mobID = PlayerManager.Instance.GetMobID();

		//Generate BloodType and DNA
		DNABloodType = new DNAandBloodType();
	}


	[RightClickMethod]
	public void PrintHealth()
	{
		foreach (var ImplantLis in ImplantList)
		{
			Logger.Log(ImplantLis.name + "\n" + "Byrne >" + ImplantLis.Burn + " Brute > " + ImplantLis.Brute +
			           " Oxy > " + ImplantLis.Oxy + " Cellular > " + ImplantLis.Cellular + " Stamina > " +
			           ImplantLis.Stamina + " Toxin > " + ImplantLis.Toxin + " DamageEfficiencyMultiplier > " + ImplantLis.DamageEfficiencyMultiplier);
		}
	}

	[RightClickMethod]
	public void DODMG()
	{
		var bit =  RootBodyPartContainers.PickRandom();
		bit.TakeDamage(null, 1, AttackType.Melee, DamageType.Brute);
	}


	[RightClickMethod]
	public void TestChemistry()
	{
		circulatorySystem.UseBloodPool.Add(Cem, 20);


	}


	/// <summary>
	/// Adds a new implant to the health master.
	/// This is NOT how implants should be added, it is called automatically by the body part container system!
	/// </summary>
	/// <param name="implant"></param>
	public void AddNewImplant(BodyPart implant)
	{
		implantList.Add(implant);
	}

	public void RemoveImplant(BodyPart implantBase)
	{
		implantList.Remove(implantBase);
	}


	public override void OnStartClient()
	{
		base.OnStartClient();
		EnsureInit();
		StartCoroutine(WaitForClientLoad());
	}

	private void UpdateMe()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			foreach (var implant in RootBodyPartContainers)
			{
				implant.ImplantUpdate(this);
			}
		}
		//do Separate delayed blood update
	}

	private void PeriodicUpdate()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			foreach (var implant in RootBodyPartContainers)
			{
				implant.ImplantPeriodicUpdate(this);
			}

			fireStacksDamage();
			CalculateRadiationDamage();
			CalculateOverallHealth();

		}
	}

	/// <summary>
	/// Radiation damage Calculations
	/// </summary>
	[Server]
	public void CalculateRadiationDamage()
	{
		var RadLevel = (registerTile.Matrix.GetRadiationLevel(registerTile.LocalPosition) * (tickRate / 5f) / 6);
		if (RadLevel == 0) return;
		ApplyDamageAll( null, RadLevel * 0.001f, AttackType.Rad, DamageType.Radiation);

	}


	public void fireStacksDamage()
	{
		if (fireStacks > 0)
		{
			//TODO: Burn clothes (see species.dm handle_fire)
			ApplyDamageAll(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Fire, DamageType.Burn, true);
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
	}

	public float GetOxyDamage()
	{
		return brain.Oxy;
	}

	public float GetTotalBruteDamage()
	{
		float toReturn = 0;
		foreach (var implant in implantList)
		{
			if (implant.DamageContributesToOverallHealth == false) continue;
			toReturn -= implant.Brute;
		}

		return toReturn;
	}


	public float GetTotalBurnDamage()
	{
		float toReturn = 0;
		foreach (var implant in implantList)
		{
			if (implant.DamageContributesToOverallHealth == false) continue;
			toReturn -= implant.Burn;
		}

		return toReturn;
	}



	public void CalculateOverallHealth()
	{
		float CurrentHealth = maxHealth;
		foreach (var implant in implantList)
		{
			if (implant.DamageContributesToOverallHealth == false) continue;
			CurrentHealth -= implant.TotalDamageWithoutOxyClone;
		}

		CurrentHealth -= brain.Oxy; //Assuming has brain
		overallHealth = CurrentHealth;

		if (overallHealth < -100)
		{
			bool hasAllHeartAttack = true;
			foreach (var Implant in ImplantList)
			{
				var heart = Implant as Heart;
				if (heart != null)
				{
					if (heart.HeartAttack == false)
					{
						hasAllHeartAttack = false;
						if (ConsciousState != ConsciousState.UNCONSCIOUS)
						{
							ConsciousState = ConsciousState.UNCONSCIOUS;
						}
						break;
					}
				}
			}

			if (hasAllHeartAttack)
			{
				ConsciousState = ConsciousState.DEAD;
			}

		}
		else if (overallHealth < -50)
		{
			if (ConsciousState != ConsciousState.UNCONSCIOUS)
			{
				ConsciousState = ConsciousState.UNCONSCIOUS;
			}
		}
		else if (overallHealth < 0)
		{
			if (ConsciousState != ConsciousState.BARELY_CONSCIOUS)
			{
				ConsciousState = ConsciousState.BARELY_CONSCIOUS;
			}
		}
		else
		{
			if (ConsciousState != ConsciousState.CONSCIOUS)
			{
				ConsciousState = ConsciousState.CONSCIOUS;
			}
		}
		//Logger.Log("overallHealth >" + overallHealth  +  " ConsciousState > " + ConsciousState);
		// Logger.Log("NutrimentLevel >" + NutrimentLevel);
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
	}


	// This is the DNA SyncVar hook
	private void DNASync(string oldDNA, string updatedDNA)
	{
		EnsureInit();
		DNABloodTypeJSON = updatedDNA;
		DNABloodType = JsonUtility.FromJson<DNAandBloodType>(updatedDNA);
	}

	/// <summary>
	///  Apply Damage to the whole body of this Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount. will be distributed evenly across all bodyparts</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	/// <param name="damageSplit">Should the damage be divided by number of body parts or applied to each body part separately</param>
	[Server]
	public void ApplyDamageAll(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType, bool damageSplit = true)
	{
		float BodyParts = 0;
		if (damageSplit)
		{
			BodyParts += RootBodyPartContainers.Sum(Container => Container.ContainsLimbs.Count());
		}

		foreach (var Container in RootBodyPartContainers)
		{
			if (damageSplit)
			{
				Container.TakeDamage(damagedBy, damage *  (Container.ContainsLimbs.Count / BodyParts) , attackType, damageType);
			}
			else
			{
				Container.TakeDamage(damagedBy, damage * Container.ContainsLimbs.Count , attackType, damageType, true);
			}

		}
	}


	/// <summary>
	///  Apply Damage to Random body part body of this Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount.</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamageToRandom(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{
		var body = RootBodyPartContainers.PickRandom();

		body.TakeDamage(damagedBy, damage, attackType, damageType);
		//TODO: Reimplement
	}

	/// <summary>
	///  Apply Damage to random bodypart of the Living thing. Server only
	/// </summary>
	/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
	/// <param name="damage">Damage Amount</param>
	/// <param name="attackType">type of attack that is causing the damage</param>
	/// <param name="damageType">The Type of Damage</param>
	[Server]
	public void ApplyDamageToBodypart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType)
	{
		//what Outer body part hit
		//How much damage is absorbed by body part
		//Body part weaknesses
		//to damage to internal components if not absorbed
		//Do this recursively

		//Guns Burns no Break bones
		//blunt maybe break bones?
		//
		//toolbox fight, Slight organ damage, Severe brutal damage
		//If over 90% increased chance of breaking bones
		//if limb is more damaged high likelihood of breaking bones?, in critical yes and noncritical?
		//
		//Shotgun, Severe brutal damage, Some organ damage,
		//damages skin until, got through then does organ damage
		//
		//gun, Severe brutal damage, Some organ damage, Embedding can be prevented from Armour  reduce organ damage if armoured
		// pellet
		// 0.5
		// rifle round
		// 1
		// Sniper around
		// 3

		//Cutting, Severe brutal damage
		//can cut off limbs, up to a damage
		//

		//Laser, Severe Burns
		//just burning

		//crush, Broken Bones, Moderate organ damage
		//no surface damage/small

		// Healing applies to both so 100 to both
		// if it's 100 healing

		// Damage, is split across the two

		// Injection chooses one

		//TODOH
		//remove old references to Sprite directions
		//Make sure is added to the manager properly
		//brains always recoverable, even if they get nuked, imo have tight regulations on what can destroy a brain
		//Surgery should be, Versus medicine medicine slow but dependable, surgery fast but requires someone doing surgery , Two only related for internal organs
		//Remove clothing item from sprites completely useless and unneeded
		ApplyDamageToBodypart(damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize(0));
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
	public virtual void ApplyDamageToBodypart(GameObject damagedBy, float damage,
		AttackType attackType, DamageType damageType, BodyPartType bodyPartAim)
	{
		foreach (var cntainers in RootBodyPartContainers)
		{
			if (cntainers.bodyPartType == bodyPartAim)
			{
				//Assuming is only going to be one otherwise damage will be duplicated across them
				cntainers.TakeDamage(damagedBy, damage, attackType, damageType);
			}
		}
	}

	public List<BodyPart> GetBodyPartsInZone( BodyPartType bodyPartAim, bool surfaceOnly = true )
	{
		foreach (var cntainers in RootBodyPartContainers)
		{
			if (cntainers.bodyPartType == bodyPartAim)
			{
				if (surfaceOnly)
				{
					return new List<BodyPart>(cntainers.ContainsLimbs);
				}
				else
				{
					var TOReturn = new List<BodyPart>();
					foreach (var BodyPart in cntainers.ContainsLimbs)
					{
						BodyPart.GetAllBodyPartsAndItself(TOReturn);
					}

					return TOReturn;
				}

			}
		}
		return new List<BodyPart>(0);
	}

	private void TryGibbing(float damage)
	{
		//idk
		//TODO: Reimplement
	}


	public List<Stomach> GetStomachs()
	{
		var Stomachs = new List<Stomach>();
		foreach (var Implant in ImplantList)
		{
			var stomach = Implant as Stomach;
			if (stomach != null)
			{
				Stomachs.Add(stomach);
			}
		}
		return Stomachs;
	}

	/// <summary>
	/// Radiation damage Calculations
	/// </summary>
	//[Server]
	// public void CalculateRadiationDamage()
	// {
	// 	var RadLevel = (registerTile.Matrix.GetRadiationLevel(registerTile.LocalPosition) * (tickRate / 5f) / 6);
	// 	var Chest = BodyParts.First(part => part.Type == BodyPartType.Chest);
	// 	RadiationStacks += Chest.armor.GetDamage(RadLevel, AttackType.Rad);
	//
	// 	var ProcessingRadiation = RadiationStacks * 0.001f;
	// 	if (ProcessingRadiation < 20 && ProcessingRadiation > 0.5f)
	// 	{
	// 		ProcessingRadiation = 20;
	// 	}
	//
	// 	RadiationStacks -= ProcessingRadiation;
	// 	bloodSystem.ToxinLevel +=  ProcessingRadiation * 0.05f;
	//
	// 	//Natural healing
	// 	//Problems should be in the metabolic system
	// 	//but thats on players only
	// 	bloodSystem.ToxinLevel -= 0.01f;
	//
	// 	if (RadiationStacks < 0)
	// 	{
	// 		RadiationStacks = 0;
	// 	}
	// }


	public void ResetDamageAll()
	{
		foreach (var BP in implantList)
		{
			BP.ResetDamage();
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
		foreach (var cntainers in RootBodyPartContainers)
		{
			if (cntainers.bodyPartType == bodyPartAim)
			{
				cntainers.HealDamage(healingItem, healAmt, damageTypeToHeal);
			}
		}


		//TODO: Reimplement
	}

	[Server]
	public void Harvest()
	{
		//Reimplement

		//Gib();
	}

	[Server]
	protected virtual void Gib()
	{
		//TODO: Reimplement

		//never destroy players!
		Despawn.ServerSingle(gameObject);
	}

	/// ---------------------------
	/// CRIT + DEATH METHODS
	/// ---------------------------
	///Death from other causes
	public virtual void Death()
	{
		OnDeathActions();
		//TODO: Reimplemenmt
	}

	protected abstract void OnDeathActions();

	public virtual void AddSickness(Sickness sickness)
	{
		//TODO: Reimplement
	}

	public void UpdateClientHealthStats(float overallHealth)
	{
		//TODO: Reimplement
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

	public void OnFullyInitialised(Action TODO)
	{
		StartCoroutine(WaitForPlayerinitialisation(TODO));
	}

	public IEnumerator WaitForPlayerinitialisation(Action TODO)
	{
		yield return null;
		TODO.Invoke();
	}



	/// <summary>
	/// Updates the blood health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBloodStats(int heartRate, float bloodVolume, float oxygenDamage, float toxinLevel)
	{
		//TODO: Reimplement bloodSystem.UpdateClientBloodStats(heartRate, bloodVolume, oxygenDamage, toxinLevel);
	}

	/// <summary>
	/// Updates the brain health stats from the server via NetMsg
	/// </summary>
	public void UpdateClientBrainStats(bool isHusk, int brainDamage)
	{
		//TODO: Reimplement
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

	public void Extinguish()
	{
		SyncFireStacks(fireStacks, 0);
	}

	public virtual string GetExamineText()
	{
		return "Weeee";
	}
}