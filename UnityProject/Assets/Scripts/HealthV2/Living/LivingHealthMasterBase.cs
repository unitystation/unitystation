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
using ScriptableObjects.Gun;
using UnityEngine;

namespace HealthV2
{
	/// <Summary>
	/// The required component for all living creatures.
	/// Monitors and controls all things health, organs, and limbs.
	/// Equivalent to the old LivingHealthBehaviour
	/// </Summary>
	[RequireComponent(typeof(HealthStateController))]
	[RequireComponent(typeof(MobSickness))]
	public abstract class LivingHealthMasterBase : NetworkBehaviour, IFireExposable, IExaminable
	{
		/// <summary>
		/// Server side, each mob has a different one and never it never changes
		/// </summary>
		public int mobID { get; private set; }

		/// <summary>
		/// Rate at which periodic damage, such as radiation, should be applied
		/// </summary>
		private float tickRate = 1f;

		private float tick = 0;

		/// Amount of blood avaiable in the circulatory system, currently unimplemented
		//public float AvailableBlood = 0;

		/// <summary>
		/// The Register Tile of the living creature
		/// </summary>
		public RegisterTile RegisterTile { get; private set; }

		/// <summary>
		/// The amount of damage taken per tick per stack of fire
		/// </summary>
		private static readonly float DAMAGE_PER_FIRE_STACK = 0.08f;

		/// <summary>
		/// Returns the current conscious state of the creature
		/// </summary>
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
				}
			}
		}

		/// <summary>
		/// Event for when the consciousness state of the creature changes, eg becoming unconscious or dead
		/// </summary>
		[NonSerialized] public ConsciousStateEvent OnConsciousStateChangeServer = new ConsciousStateEvent();

		/// <summary>
		/// Returns true if the creature's current conscious state is dead
		/// </summary>
		public bool IsDead => ConsciousState == ConsciousState.DEAD;

		/// <summary>
		/// Returns true if the creature's current conscious state is unconscious
		/// </summary>
		public bool IsCrit => ConsciousState == ConsciousState.UNCONSCIOUS;

		/// <summary>
		/// Returns true if the creature's current conscious state is barely conscious
		/// </summary>
		public bool IsSoftCrit => ConsciousState == ConsciousState.BARELY_CONSCIOUS;

		/// <summary>
		/// The current body type of the creature
		/// </summary>
		public BodyType BodyType = BodyType.NonBinary;

		/// <summary>
		/// The difference between network time and player time for the entity if its a player
		/// Used to calculate amount of time to delay health changes if client is behind server
		/// </summary>
		public float RTT;

		/// <summary>
		/// The health of the creature when it has taken no damage
		/// </summary>
		[SerializeField] private float maxHealth = 100;

		public float MaxHealth
		{
			get => maxHealth;
		}

		/// <summary>
		/// The current overall health of the creature.
		/// -15 is barely conscious, -50 unconscious, and -100 is dying/dead
		/// </summary>
		public float OverallHealth => healthStateController.OverallHealth;

		/// <summary>
		/// List of all of the body parts of the creature, currently unimplemented
		/// </summary>
		//[SerializeField] [Tooltip("These are the things that will hold all our organs and implants.")]
		//private List<BodyPart> bodyPartContainers;

		/// <summary>
		/// The creature's Circulatory System
		/// </summary>
		[CanBeNull]
		public CirculatorySystemBase CirculatorySystem { get; private set; }

		public Brain brain;

		/// <summary>
		/// The creature's Respiratory System
		/// </summary>
		[CanBeNull]
		public RespiratorySystemBase RespiratorySystem { get; private set; }

		/// <summary>
		/// The creature's Metabolism System, currently unimplemented
		/// </summary>
		[CanBeNull]
		public MetabolismSystemV2 Metabolism { get; private set; }

		/// <summary>
		/// A list of all body parts of the creature
		/// </summary>
		public List<BodyPart> ImplantList = new List<BodyPart>();

		/// <summary>
		/// A list of all body part containers of the creature.
		/// A body part container is a grouping of like body parts (legs, arms, eyes, etc)
		/// </summary>
		public List<RootBodyPartContainer> RootBodyPartContainers = new List<RootBodyPartContainer>();

		/// <summary>
		/// Event that fires when damage is applied to the creature
		/// </summary>
		public event Action<GameObject> ApplyDamageEvent;

		/// <summary>
		/// Event that fires when the creature dies
		/// </summary>
		public event Action OnDeathNotifyEvent;

		// FireStacks note: It's called "stacks" but it's really just a floating point value that
		// can go up or down based on possible sources of being on fire. Max seems to be 20 in tg.
		private float fireStacks => healthStateController.FireStacks;

		/// <summary>
		/// How on fire we are, same as tg fire_stacks. 0 = not on fire.
		/// Exists client side - synced with server.
		/// </summary>
		public float FireStacks => fireStacks;

		private float maxFireStacks = 5f;
		private bool maxFireStacksReached = false;

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

		/// <summary>
		/// The current hunger state of the creature, currently always returns normal
		/// </summary>
		public HungerState HungerState => CalculateHungerState();

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
		/// Current sicknesses status of the creature and it's current stage
		/// </summary>
		private MobSickness mobSickness = null;

		/// <summary>
		/// List of sicknesses that creature has gained immunity to
		/// </summary>
		private List<Sickness> immunedSickness;

		public PlayerScript PlayerScriptOwner;

		public virtual void Awake()
		{
			EnsureInit();
			if(PlayerScriptOwner == null)
			{
				PlayerScriptOwner = this.gameObject.Player().Script;
			}
		}

		void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			UpdateManager.Add(PeriodicUpdate, 1f);
		}

		void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
		}

		public virtual void EnsureInit()
		{
			if (RegisterTile) return;
			RegisterTile = GetComponent<RegisterTile>();
			RespiratorySystem = GetComponent<RespiratorySystemBase>();
			CirculatorySystem = GetComponent<CirculatorySystemBase>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			healthStateController = GetComponent<HealthStateController>();
			immunedSickness = new List<Sickness>();
			mobSickness = GetComponent<MobSickness>();

			foreach (var implant in ImplantList)
			{
				//Debug.Log(implant.gameObject.name);
				implant.HealthMaster = this;
				implant.Initialisation();
			}
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


		public Reagent CHem;

		[RightClickMethod]
		public void InjectChemical()
		{
			CirculatorySystem.ReadyBloodPool.Add(CHem, 5);
		}

		[RightClickMethod]
		public void DODMG()
		{
			var bit = RootBodyPartContainers.PickRandom();
			bit.TakeDamage(null, 1, AttackType.Melee, DamageType.Brute);
		}

		/// <summary>
		/// Adds a new body part to the health master.
		/// This is NOT how body parts should be added, it is called automatically by the body part container system!
		/// </summary>
		/// <param name="implant"></param>
		public void AddNewImplant(BodyPart implant)
		{
			ImplantList.Add(implant);
		}

		/// <summary>
		/// Removes a body part from the health master
		/// </summary>
		public void RemoveImplant(BodyPart implantBase)
		{
			ImplantList.Remove(implantBase);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			EnsureInit();
		}

		//Server Side only
		private void UpdateMe()
		{
			foreach (var implant in RootBodyPartContainers)
			{
				implant.ImplantUpdate();
			}
			//do Separate delayed blood update
		}

		//Server Side only
		private void PeriodicUpdate()
		{
			foreach (var implant in RootBodyPartContainers)
			{
				implant.ImplantPeriodicUpdate();
			}

			fireStacksDamage();
			CalculateRadiationDamage();
			CalculateOverallHealth();
		}

		/// <summary>
		/// Calculates and applies radiation damage
		/// </summary>
		[Server]
		public void CalculateRadiationDamage()
		{
			var radLevel = (RegisterTile.Matrix.GetRadiationLevel(RegisterTile.LocalPosition) * (tickRate / 5f) / 6);

			if (radLevel == 0) return;

			ApplyDamageAll(null, radLevel * 0.001f, AttackType.Rad, DamageType.Radiation);
		}

		/// <summary>
		/// Applys damage from fire stacks and handles their effects and decay
		/// </summary>
		public void fireStacksDamage()
		{
			if (fireStacks > 0)
			{
				//TODO: Burn clothes (see species.dm handle_fire)
				ApplyDamageAll(null, fireStacks * DAMAGE_PER_FIRE_STACK, AttackType.Fire, DamageType.Burn, true);
				//gradually deplete fire stacks
				healthStateController.SetFireStacks(fireStacks - 0.1f);
				//instantly stop burning if there's no oxygen at this location
				MetaDataNode node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile.LocalPositionClient);
				if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
				{
					healthStateController.SetFireStacks(0);
				}

				RegisterTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition());
			}
		}

		/// <summary>
		/// Returns the current amount of oxy damage the brain has taken
		/// </summary>
		public float GetOxyDamage()
		{
			return brain.RelatedPart.Oxy;
		}

		/// <summary>
		/// Returns the the sum of all brute damage taken by body parts
		/// </summary>
		public float GetTotalBruteDamage()
		{
			float toReturn = 0;
			foreach (var implant in ImplantList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				toReturn -= implant.Brute;
			}

			return toReturn;
		}

		/// <summary>
		/// Returns the the sum of all burn damage taken by body parts
		/// </summary>
		public float GetTotalBurnDamage()
		{
			float toReturn = 0;
			foreach (var implant in ImplantList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				toReturn -= implant.Burn;
			}

			return toReturn;
		}

		/// <summary>
		/// Returns the total amount of blood in the body of the type of blood the body should have
		/// </summary>
		public float GetTotalBlood()
		{
			return GetSpareBlood() + GetImplantBlood();
		}

		/// <summary>
		/// Returns the total amount of blood contained within body parts
		/// </summary>
		public float GetImplantBlood()
		{
			float toReturn = 0;
			foreach (var implant in ImplantList)
			{
				toReturn += implant.BloodContainer[CirculatorySystem.BloodType];
			}

			return toReturn;
		}

		/// <summary>
		/// Returns the total amount of 'spare' blood outside of the organs
		/// </summary>
		public float GetSpareBlood()
		{
			return CirculatorySystem.UsedBloodPool[CirculatorySystem.BloodType]
			       + CirculatorySystem.ReadyBloodPool[CirculatorySystem.BloodType];
		}

		/// <summary>
		/// Unimplemented
		/// </summary>
		public void AddBodyPartToRoot()
		{
		}

		/// <summary>
		/// Returns true if the creature has the given body part of a type targetable by the UI
		/// </summary>
		/// <param name="bodyPartType">The type of Body Part to check</param>
		/// <param name="surfaceOnly">Checks only external bodyparts if true, all if false (default)</param>
		public bool HasBodyPart(BodyPartType bodyPartType, bool surfaceOnly = false)
		{
			foreach (var bodyPart in ImplantList)
			{
				if (bodyPart.BodyPartType == bodyPartType)
				{
					if (surfaceOnly && bodyPart.IsSurface == false)
					{
						continue;
					}

					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Updates overall health based on damage sustained by body parts thus far.
		/// Also updates consciousness status and will initiate a heart attack if low enough.
		/// </summary>
		public void CalculateOverallHealth()
		{
			float currentHealth = maxHealth;
			foreach (var implant in ImplantList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				currentHealth -= implant.TotalDamageWithoutOxyCloneRadStam;
			}

			if (brain == null ||  brain.RelatedPart.Health < -100)
			{
				currentHealth -= 200;
				healthStateController.SetOverallHealth(currentHealth);
				CheckHeartStatus();
				return;
			}
			else
			{
				currentHealth -= brain.RelatedPart.Oxy;
			}


			//Sync health
			healthStateController.SetOverallHealth(currentHealth);

			if (currentHealth < -100)
			{
				CheckHeartStatus();
			}
			else if (currentHealth < -50)
			{
				SetConsciousState(ConsciousState.UNCONSCIOUS);
			}
			else if (currentHealth < 0)
			{
				SetConsciousState(ConsciousState.BARELY_CONSCIOUS);
			}
			else
			{
				SetConsciousState(ConsciousState.CONSCIOUS);
			}

			//Logger.Log("overallHealth >" + overallHealth  +  " ConsciousState > " + ConsciousState);
			// Logger.Log("NutrimentLevel >" + NutrimentLevel);
		}

		private void CheckHeartStatus()
		{
			bool hasAllHeartAttack = true;
			foreach (var Implant in ImplantList)
			{
				foreach (var bodyPartModification in Implant.BodyPartModifications)
				{
					if (bodyPartModification is Heart heart && heart.HeartAttack == false)
					{
						hasAllHeartAttack = false;
						SetConsciousState(ConsciousState.UNCONSCIOUS);

						break;
					}
				}
			}

			if (hasAllHeartAttack)
			{
				Death();
			}
		}

		private void SetConsciousState(ConsciousState NewConsciousState)
		{
			if (ConsciousState != NewConsciousState)
			{
				ConsciousState = NewConsciousState;
			}
		}

		/// <summary>
		/// Apply damage to the all body parts of the creature. Server only
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
			if (damageSplit)
			{
				float bodyParts = RootBodyPartContainers.Sum(Container => Container.ContainsLimbs.Count());
				damage /= bodyParts;
			}

			foreach (var Container in RootBodyPartContainers)
			{
				if (damageSplit)
				{
					Container.TakeDamage(damagedBy, damage * Container.ContainsLimbs.Count, attackType, damageType);
				}
				else
				{
					Container.TakeDamage(damagedBy, damage, attackType, damageType, damageSplit: true);
				}
			}

			if (damageType == DamageType.Brute)
			{
				//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
				//EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
			}
		}

		/// <summary>
		/// Apply damage to a random body part body of the creature. Server only
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

			if (damageType == DamageType.Brute)
			{
				//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
				//EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
			}
		}

		/// <summary>
		/// Apply damage to a random bodypart of the creature. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage Amount</param>
		/// <param name="attackType">type of attack that is causing the damage</param>
		/// <param name="damageType">The Type of Damage</param>
		[Server]
		public void ApplyDamageToBodyPart(GameObject damagedBy, float damage,
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
			ApplyDamageToBodyPart(damagedBy, damage, attackType, damageType, BodyPartType.Chest.Randomize(0));
		}

		/// <summary>
		/// Apply Damage to a specified body part of the creature. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damageData">Damage data</param>
		/// <param name="bodyPartAim">Body Part that is affected</param>
		/// <param name="armorPenetration">How well or poorly it will break through different types of armor</param>
		public virtual void ApplyDamageToBodyPart(
			GameObject damagedBy,
			DamageData damageData,
			BodyPartType bodyPartAim
		)
		{
			ApplyDamageToBodyPart(
				damagedBy,
				damageData.Damage,
				damageData.AttackType,
				damageData.DamageType,
				bodyPartAim,
				damageData.ArmorPenetration
			);
		}

		/// <summary>
		///  Apply Damage to a specified body part of the creature. Server only
		/// </summary>
		/// <param name="damagedBy">The player or object that caused the damage. Null if there is none</param>
		/// <param name="damage">Damage Amount</param>
		/// <param name="attackType">type of attack that is causing the damage</param>
		/// <param name="damageType">The Type of Damage</param>
		/// <param name="bodyPartAim">Body Part that is affected</param>
		[Server]
		public virtual void ApplyDamageToBodyPart(
			GameObject damagedBy,
			float damage,
			AttackType attackType,
			DamageType damageType,
			BodyPartType bodyPartAim,
			float armorPenetration = 0
		)
		{
			LastDamageType = damageType;
			LastDamagedBy = damagedBy;

			foreach (var bodyPartContainer in RootBodyPartContainers)
			{
				if (bodyPartContainer.BodyPartType == bodyPartAim)
				{
					//Assuming is only going to be one otherwise damage will be duplicated across them
					bodyPartContainer.TakeDamage(damagedBy, damage, attackType, damageType, armorPenetration);
				}
			}
		}

		/// <summary>
		/// Applys Trauma Damage to a specified body part of the creature. Server only
		/// </summary>
		/// <param name="aimedBodyPart">Which body part do we target?</param>
		/// <param name="damage">The Trauma damage value</param>
		/// <param name="damageType">TraumaticDamageType enum, can be Slash, Burn and/or Pierce.</param>
		[Server]
		public virtual void ApplyTraumaDamage(BodyPartType aimedBodyPart, float damage, TramuticDamageTypes damageType)
		{
			RootBodyPartContainer aimedPartContainer = null;
			foreach (RootBodyPartContainer container in RootBodyPartContainers)
			{
				if (container.BodyPartType == aimedBodyPart)
				{
					aimedPartContainer = container;
				}
			}

			if(aimedPartContainer == null)
			{
				Logger.LogError($"[LivingHealthBase/{name}] - Unable to find body part container. Skipping Trauma Damage.");
				return;
			}

			aimedPartContainer.TakeTraumaDamage(damage, damageType);
		}

		/// <summary>
		/// Gets all body parts in a zone targetable by the UI (head, chest, left arm, etc)
		/// </summary>
		/// <param name="bodyPartAim">The body part being aimed at</param>
		/// <param name="surfaceOnly">Returns only external bodyparts if true (default), all if false</param>
		/// <returns>List of BodyParts</returns>
		public List<BodyPart> GetBodyPartsInZone(BodyPartType bodyPartAim, bool surfaceOnly = true)
		{
			foreach (var cntainers in RootBodyPartContainers)
			{
				if (cntainers.BodyPartType == bodyPartAim)
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

		/// <summary>
		/// Gets all body part containers (arms, legs, eyes, etc) in a zone targetable by the UI
		/// </summary>
		/// <param name="bodyPartAim">The body part being aimed at</param>
		/// <returns>List of RootBodyPartContainers</returns>
		public RootBodyPartContainer GetRootBodyPartInZone(BodyPartType bodyPartAim)
		{
			foreach (var cntainers in RootBodyPartContainers)
			{
				if (cntainers.BodyPartType == bodyPartAim)
				{
					return cntainers;
				}
			}

			return null;
		}

		/// <summary>
		/// Only does damage to the first layer
		/// </summary>
		/// <returns></returns>
		public bool ZoneHasDamageOf(BodyPartType bodyPartAim, DamageType SpecifiedType)
		{
			foreach (var cntainers in RootBodyPartContainers)
			{
				if (cntainers.BodyPartType == bodyPartAim)
				{
					foreach (var bodyPart in cntainers.ContainsLimbs)
					{
						if (bodyPart.Damages[(int) SpecifiedType] > 0)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gibs the creature if possible, not implemented
		/// </summary>
		private void TryGibbing(float damage)
		{
			//idk
			//TODO: Reimplement
		}

		/// <summary>
		/// Gets a list of all the stomachs in the creature
		/// </summary>
		/// <returns>List of Stomachs</returns>
		public List<Stomach> GetStomachs()
		{
			var Stomachs = new List<Stomach>();
			foreach (var Implant in ImplantList)
			{
				foreach (var bodyPartModification in Implant.BodyPartModifications)
				{
					var stomach = bodyPartModification as Stomach;
					if (stomach != null)
					{
						Stomachs.Add(stomach);
					}
				}
			}

			return Stomachs;
		}

		/// <summary>
		/// Resets all damage values of all body parts to 0
		/// </summary>
		public void ResetDamageAll()
		{
			foreach (var bodyPart in ImplantList)
			{
				bodyPart.ResetDamage();
			}

		}


		/// <summary>
		/// Does the body part we're targeting suffer from traumatic damage?
		/// </summary>
		/// <param name="damageTypeToGet">Trauma damage type</param>
		/// <param name="partType">targted body part.</param>
		/// <returns></returns>
		public bool HasTraumaDamage(BodyPartType partType)
		{
			foreach(var container in RootBodyPartContainers)
			{
				if(container.BodyPartType == partType)
				{
					foreach(BodyPart part in container.ContainsLimbs)
					{
						if (part.GetCurrentBurnDamage() > 0) return true;
						if (part.GetCurrentSlashDamage() > 0) return true;
						if (part.GetCurrentPierceDamage() > 0) return true;
					}
				}
			}
			return false;
		}

		public void HealTraumaDamage(float healAmount, BodyPartType targetBodyPartToHeal, TramuticDamageTypes typeToHeal)
		{
			foreach(var container in RootBodyPartContainers)
			{
				if(container.BodyPartType == targetBodyPartToHeal)
				{
					container.HealTraumaDamage(healAmount, typeToHeal);
				}
			}
		}

		/// <summary>
		/// Revives a dead player to full health.
		/// </summary>
		public void RevivePlayerToFullHealth(PlayerScript player)
		{
			Extinguish(); //Remove any fire on them.
			ResetDamageAll(); //Bring their entire body parts that are on them in good shape.
			healthStateController.SetOverallHealth(maxHealth); //Set the player's overall health to their race's maxHealth.
			foreach (var BodyPart in ImplantList) //Restart their heart.
			{
				foreach (var bodyPartModification in BodyPart.BodyPartModifications)
				{
					if (bodyPartModification is Heart heart)
					{
						heart.HeartAttack = false;
						heart.CanTriggerHeartAttack = false;
						heart.CurrentPulse = 100;
						heart.DoHeartBeat(this);
					}
				}
			}
			CalculateOverallHealth(); //This makes the player alive and concision.
			player.playerMove.allowInput = true; //Let them interact with the world again.
		}

		/// <summary>
		/// Apply healing to the creature. Server Only
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
				if (cntainers.BodyPartType == bodyPartAim)
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
		public virtual void Gib()
		{
			Death();
			_ = SoundManager.PlayAtPosition(SingletonSOSounds.Instance.Slip, gameObject.transform.position, gameObject); //TODO: replace with gibbing noise
			CirculatorySystem.Bleed(GetTotalBlood());
			foreach(RootBodyPartContainer container in RootBodyPartContainers.ToArray())
			{
				container.RemoveLimbs(false);
			}
		}

		/// ---------------------------
		/// CRIT + DEATH METHODS
		/// ---------------------------
		///<Summary>
		/// Kills the creature, used for causes of death other than damage.
		/// Currently not fully implemented
		///</Summary>
		public virtual void Death()
		{
			var HV2 = (this as PlayerHealthV2);
			if (HV2 != null)
			{
				if (HV2.PlayerScript.OrNull()?.playerMove.OrNull()?.allowInput != null)
				{
					HV2.PlayerScript.playerMove.allowInput = false;
				}

			}

			SetConsciousState(ConsciousState.DEAD);
			OnDeathActions();
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
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

		public void OnExposed(FireExposure exposure)
		{
			ChangeFireStacks(1f);
			ApplyDamageAll(null, 0.25f, AttackType.Fire, DamageType.Burn, false);
		}

		/// <summary>
		/// Adjusts the amount of fire stacks, to a min of 0 (not on fire) and a max of maxFireStacks
		/// </summary>
		/// <param name="deltaValue">The amount to adjust the stacks by, negative if reducing positive if increasing</param>
		public void ChangeFireStacks(float deltaValue)
		{
			if (fireStacks + deltaValue < 0)
			{
				healthStateController.SetFireStacks(0);
			}
			else if (fireStacks + deltaValue > maxFireStacks)
			{
				healthStateController.SetFireStacks(maxFireStacks);
			}
			else
			{
				healthStateController.SetFireStacks(fireStacks + deltaValue);
			}
		}

		/// <summary>
		/// Removes all Fire Stacks
		/// </summary>
		public void Extinguish()
		{
			healthStateController.SetFireStacks(0);
		}

		#region Examine


		public string Examine(Vector3 worldPos = default(Vector3))
		{
			// This is for mobs, player uses ExaminablePlayer
			// Which can call GetExamineText if the player is too far away
			if (this is PlayerHealthV2)
			{
				return default;
			}

			return GetExamineText();
		}

		/// <summary>
		/// Gets the appropriate examine text based on the creature's health state
		/// </summary>
		/// <returns>String describing the creature</returns>
		public string GetExamineText(PlayerScript script = null)
		{
			var theyPronoun = script == null ? "It" : script.characterSettings.TheyPronoun(script);
			var healthString = new StringBuilder($"{theyPronoun} is ");

			if (IsDead)
			{
				healthString.Insert(0, "<color=#b495bf>");
				healthString.Append("limp and unresponsive; there are no signs of life");

				if (script != null && script.HasSoul == false)
				{
					healthString.Append($" and {script.characterSettings.TheirPronoun(script)} soul has departed");
				}

				healthString.Append("...</color>");

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

			if (RespiratorySystem != null && RespiratorySystem.IsSuffocating)
			{
				healthString.Append($" {theyPronoun}'s having trouble breathing.");
			}

			// On fire?
			if (FireStacks > 0)
			{
				healthString.Append($" And {theyPronoun}'s on fire!");
			}

			//Alive but not in body
			if (script != null && script.HasSoul == false)
			{
				healthString.Append($"<color=#b495bf>\n{theyPronoun} has a blank, absent-minded stare and appears completely unresponsive to anything. {theyPronoun} may snap out of it soon.</color>");
			}

			return healthString.ToString();
		}

		#endregion

		#region Sickness

		/// <summary>
		/// Adds a sickness to the creature if it doesn't already have it and isn't dead or immune
		/// </summary>
		/// <param name="sickness">The sickness to add</param>
		public void AddSickness(Sickness sickness)
		{
			if (IsDead)
				return;

			if ((!mobSickness.HasSickness(sickness)) && (!immunedSickness.Contains(sickness)))
				mobSickness.Add(sickness, Time.time);
		}

		/// <summary>
		/// Removes the specified sickness from the creature, healing it
		/// The creature will not be immune, to immunize it as well use ImmuneSickness
		/// </summary>
		/// <param name="sickness">The sickness to remove</param>
		/// <remarks>Thread safe</remarks>
		public void RemoveSickness(Sickness sickness)
		{
			SicknessAffliction sicknessAffliction =
				mobSickness.sicknessAfflictions.FirstOrDefault(p => p.Sickness == sickness);

			if (sicknessAffliction != null)
				sicknessAffliction.Heal();
		}

		/// <summary>
		/// Removes the specified sickness from the creature, healing it.
		/// Also immunizes it for the current round, to only cure it use RemoveSickness.
		/// </summary>
		/// <param name="sickness">The sickness to remove</param>
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
