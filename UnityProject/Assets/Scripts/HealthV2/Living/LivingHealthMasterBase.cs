using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Systems.Atmospherics;
using Chemistry;
using Health.Sickness;
using JetBrains.Annotations;
using Mirror;
using UnityEngine;

namespace HealthV2
{
	[RequireComponent(typeof(HealthStateController))]
	[RequireComponent(typeof(MobSickness))]
	public abstract class LivingHealthMasterBase : NetworkBehaviour
	{
		/// <summary>
		/// Server side, each mob has a different one and never it never changes
		/// </summary>
		public int mobID { get; private set; }

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
			get => healthStateController.ConsciousState;
			protected set
			{
				ConsciousState oldState = healthStateController.ConsciousState;
				if (value != oldState)
				{
					healthStateController.SetConsciousState(value);

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

		public BodyType BodyType = BodyType.NonBinary;

		//I don't know what this is. It was in the first Health system.
		public float RTT;

		[SerializeField] private float maxHealth = 100; //player stuff

		public float OverallHealth => healthStateController.OverallHealth;

		[SerializeField] [Tooltip("These are the things that will hold all our organs and implants.")]
		private List<BodyPart> bodyPartContainers;

		[CanBeNull] private CirculatorySystemBase circulatorySystem;
		public CirculatorySystemBase CirculatorySystem => circulatorySystem;

		private Brain brain;

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
		private float fireStacks => healthStateController.FireStacks;

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
		public ObjectBehaviour ObjectBehaviour => objectBehaviour;

		private HealthStateController healthStateController;
		public HealthStateController HealthStateController => healthStateController;

		protected DamageType LastDamageType;

		protected GameObject LastDamagedBy;

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

		/// <summary>
		/// Current sicknesses status of the player and their current stage.
		/// </summary>
		private MobSickness mobSickness = null;

		/// <summary>
		/// List of sicknesses that player has gained immunity.
		/// </summary>
		private List<Sickness> immunedSickness;

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

		public virtual void EnsureInit()
		{
			if (registerTile) return;
			registerTile = GetComponent<RegisterTile>();
			respiratorySystem = GetComponent<RespiratorySystemBase>();
			circulatorySystem = GetComponent<CirculatorySystemBase>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			healthStateController = GetComponent<HealthStateController>();
			immunedSickness = new List<Sickness>();
			mobSickness = GetComponent<MobSickness>();
			//Always include blood for living entities:
		}

		public void Setbrain(Brain _brain)
		{
			brain = _brain;
		}

		public override void OnStartServer()
		{
			EnsureInit();
			mobID = PlayerManager.Instance.GetMobID();

			//Generate BloodType and DNA
			healthStateController.SetDNA(new DNAandBloodType());
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
			var radLevel = (registerTile.Matrix.GetRadiationLevel(registerTile.LocalPosition) * (tickRate / 5f) / 6);

			if (radLevel == 0) return;

			ApplyDamageAll( null, radLevel * 0.001f, AttackType.Rad, DamageType.Radiation);
		}


		public void fireStacksDamage()
		{
			if (fireStacks > 0)
			{
				//TODO: Burn clothes (see species.dm handle_fire)
				ApplyDamageAll(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Fire, DamageType.Burn, true);
				//gradually deplete fire stacks
				healthStateController.SetFireStacks(fireStacks - 0.1f);
				//instantly stop burning if there's no oxygen at this location
				MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionClient);
				if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
				{
					healthStateController.SetFireStacks(0);
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

		public void AddBodyPartToRoot()
		{

		}

		public bool HasBodyPart(BodyPartType bodyPartType, bool surfaceOnly = false)
		{
			foreach (var bodyPart in implantList)
			{
				if (bodyPart.bodyPartType == bodyPartType)
				{
					if (surfaceOnly && bodyPart.isSurface == false)
					{
						continue;
					}
					return true;
				}
			}

			return false;
		}

		public void CalculateOverallHealth()
		{
			float currentHealth = maxHealth;
			foreach (var implant in implantList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				currentHealth -= implant.TotalDamageWithoutOxyCloneRadStam;
			}

			currentHealth -= brain.Oxy; //Assuming has brain

			//Sync health
			healthStateController.SetOverallHealth(currentHealth);

			if (currentHealth < -100)
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
			else if (currentHealth < -50)
			{
				if (ConsciousState != ConsciousState.UNCONSCIOUS)
				{
					ConsciousState = ConsciousState.UNCONSCIOUS;
				}
			}
			else if (currentHealth < 0)
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

			EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, BloodSplatType.red);
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

			EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, BloodSplatType.red);
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
			LastDamageType = damageType;
			LastDamagedBy = damagedBy;

			foreach (var bodyPartContainer in RootBodyPartContainers)
			{
				if (bodyPartContainer.bodyPartType == bodyPartAim)
				{
					//Assuming is only going to be one otherwise damage will be duplicated across them
					bodyPartContainer.TakeDamage(damagedBy, damage, attackType, damageType);
				}
			}

			EffectsFactory.BloodSplat(transform.position, BloodSplatSize.large, BloodSplatType.red);
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


		public RootBodyPartContainer GetRootBodyPartInZone(BodyPartType bodyPartAim)
		{
			foreach (var cntainers in RootBodyPartContainers)
			{
				if (cntainers.bodyPartType == bodyPartAim)
				{
					return cntainers;
				}
			}

			return null;
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

		public void ResetDamageAll()
		{
			foreach (var bodyPart in implantList)
			{
				bodyPart.ResetDamage();
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

			Gib();
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
			healthStateController.SetFireStacks(fireStacks + deltaValue);
		}

		public void Extinguish()
		{
			healthStateController.SetFireStacks(0);
		}

		#region Examine

		public string GetExamineText()
		{
			if (this is PlayerHealthV2)
			{
				// Let ExaminablePlayer take care of this.
				return default;
			}

			// Assume animal
			var healthString = new StringBuilder("It is ");

			if (IsDead)
			{
				healthString.Append("limp and unresponsive; there are no signs of life...");

				return healthString.ToString();
			}

			healthString.Append($"{ConsciousState.ToString().ToLower().Replace("_", " ")} and ");

			var healthFraction = OverallHealth / maxHealth;
			if (healthFraction < 0.2f)
			{
				healthString.Append("heavily wounded.");
			}
			else if (healthFraction < 0.6f)
			{
				healthString.Append("wounded.");
			}
			else
			{
				healthString.Append("in good shape.");
			}

			if (respiratorySystem != null && respiratorySystem.IsSuffocating)
			{
				healthString.Append("having trouble breathing!");
			}

			// On fire?
			if (FireStacks > 0)
			{
				healthString.Append(" And is on fire!");
			}

			return healthString.ToString();
		}

		#endregion

		#region Sickness



		/// <summary>
		/// Add a sickness to the player if he doesn't already has it and isn't immuned
		/// </summary>
		/// <param name="">The sickness to add</param>
		public void AddSickness(Sickness sickness)
		{
			if (IsDead)
				return;

			if ((!mobSickness.HasSickness(sickness)) && (!immunedSickness.Contains(sickness)))
				mobSickness.Add(sickness, Time.time);
		}

		/// <summary>
		/// This will remove the sickness from the player, healing him.
		/// </summary>
		/// <remarks>Thread safe</remarks>
		public void RemoveSickness(Sickness sickness)
		{
			SicknessAffliction sicknessAffliction = mobSickness.sicknessAfflictions.FirstOrDefault(p => p.Sickness == sickness);

			if (sicknessAffliction != null)
				sicknessAffliction.Heal();
		}

		/// <summary>
		/// This will remove the sickness from the player, healing him.  This will also make him immune for the current round.
		/// </summary>
		public void ImmuneSickness(Sickness sickness)
		{
			RemoveSickness(sickness);

			if (!immunedSickness.Contains(sickness))
				immunedSickness.Add(sickness);
		}

		#endregion

		#region Electrocution

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
			ApplyDamageAll(null, damage, AttackType.Internal, DamageType.Burn);
		}

		#endregion
	}
}
