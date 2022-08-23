using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AdminCommands;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Systems.Atmospherics;
using Chemistry;
using Core.Chat;
using Health.Sickness;
using HealthV2.Living.CirculatorySystem;
using JetBrains.Annotations;
using NaughtyAttributes;
using Player;
using Newtonsoft.Json;
using ScriptableObjects.RP;
using UnityEngine.Serialization;

namespace HealthV2
{
	/// <Summary>
	/// The required component for all living creatures.
	/// Monitors and controls all things health, organs, and limbs.
	/// Equivalent to the old LivingHealthBehaviour
	/// </Summary>
	[RequireComponent(typeof(HealthStateController))]
	[RequireComponent(typeof(MobSickness))]
	public abstract class LivingHealthMasterBase : NetworkBehaviour, IFireExposable, IExaminable, IFullyHealable, IGib,
		IAreaReactionBase, IRightClickable
	{
		/// <summary>
		/// Server side, each mob has a different one and never it never changes
		/// </summary>
		public int mobID { get; private set; }

		/// <summary>
		/// Rate at which periodic damage, such as radiation, should be applied
		/// </summary>
		private float tickRate = 1f;

		/// <summary>
		/// The Register Tile of the living creature
		/// </summary>
		public RegisterTile RegisterTile { get; private set; }

		/// <summary>
		/// Returns the current conscious state of the creature
		/// </summary>
		public ConsciousState ConsciousState => healthStateController.ConsciousState;

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
		public float MaxHealth => healthStateController.MaxHealth;

		/// <summary>
		/// The current overall health of the creature.
		/// -15 is barely conscious, -50 unconscious, and -100 is dying/dead
		/// </summary>
		public float OverallHealth => healthStateController.OverallHealth;

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
		public List<BodyPart> BodyPartList = new List<BodyPart>();

		public List<BodyPart> SurfaceBodyParts = new List<BodyPart>();

		/// <summary>
		/// The storage container for the body parts
		/// </summary>
		[HideInInspector] public ItemStorage BodyPartStorage;

		[HideInInspector] public PlayerSprites playerSprites;

		// FireStacks note: It's called "stacks" but it's really just a floating point value that
		// can go up or down based on possible sources of being on fire. Max seems to be 20 in tg.
		private float fireStacks => healthStateController.FireStacks;

		/// <summary>
		/// How on fire we are, same as tg fire_stacks. 0 = not on fire.
		/// Exists client side - synced with server.
		/// </summary>
		public float FireStacks => fireStacks;

		private float maxFireStacks = 5f;

		/// <summary>
		/// Client side event which fires when this object's fire status changes
		/// (becoming on fire, extinguishing, etc...). Use this to update
		/// burning sprites.
		/// </summary>
		[NonSerialized] public FireStackEvent OnClientFireStacksChange = new FireStackEvent();

		/// <summary>
		/// How badly we're bleeding, same as tg bleed_stacks. 0 = not bleeding.
		/// Exists client side - synced with server.
		/// </summary>
		public float BleedStacks => healthStateController.BleedStacks;

		private float maxBleedStacks = 10f;

		[SerializeField, BoxGroup("PainFeedback")]
		private float painScreamDamage = 20f;

		[SerializeField, BoxGroup("PainFeedback")]
		private float painScreamCooldown = 15f;

		[SerializeField, BoxGroup("PainFeedback")]
		private EmoteSO screamEmote;

		private bool canScream = true;

		private UniversalObjectPhysics objectBehaviour;
		public UniversalObjectPhysics ObjectBehaviour => objectBehaviour;

		[SerializeField, BoxGroup("FastRegen")]
		private float fastRegenHeal = 12;

		[SerializeField, BoxGroup("FastRegen")]
		private float fastRegenThreshold = 85;


		private HealthStateController healthStateController;
		public HealthStateController HealthStateController => healthStateController;

		protected GameObject LastDamagedBy;

		private DateTime timeOfDeath;
		private DateTime TimeOfDeath => timeOfDeath;

		/// <summary>
		/// The list of the internal net ids of the body parts contained within this container
		/// </summary>
		[Tooltip("The internal net ids of the body parts contained within this")]
		public List<IntName> InternalNetIDs = new List<IntName>();

		public RootBodyPartController rootBodyPartController;


		public float BodyPartSurfaceVolume = 5;

		/// <summary>
		/// The current hunger state of the creature, currently always returns normal
		/// </summary>
		public HungerState HungerState => CalculateHungerState();

		public HungerState CalculateHungerState()
		{
			var State = HungerState.Full;
			foreach (var bodyPart in BodyPartList)
			{
				if (bodyPart.HungerState == HungerState.Full)
				{
					State = HungerState.Full;
					break;
				}

				if ((int) bodyPart.HungerState > (int) State) //TODO Add the other states
				{
					State = bodyPart.HungerState;
					if (State == HungerState.Starving)
					{
						break;
					}
				}
			}

			return State;
		}

		public BleedingState BleedingState => CalculateBleedingState();

		public BleedingState CalculateBleedingState()
		{
			var State = BleedingState.None;
			switch ((int) Math.Ceiling(BleedStacks))
			{
				case 0:
					State = BleedingState.None;
					break;
				case 1:
					State = BleedingState.VeryLow;
					break;
				case int n when n.IsBetween(2, 3):
					State = BleedingState.Low;
					break;
				case int n when n.IsBetween(4, 6):
					State = BleedingState.Medium;
					break;
				case int n when n.IsBetween(7, 8):
					State = BleedingState.High;
					break;
				case int n when n.IsBetween(9, 10):
					State = BleedingState.UhOh;
					break;
			}

			return State;
		}

		/// <summary>
		/// Current sicknesses status of the creature and it's current stage
		/// </summary>
		public MobSickness mobSickness { get; private set; } = null;

		/// <summary>
		/// List of sicknesses that creature has gained immunity to
		/// </summary>
		private List<Sickness> immunedSickness = new List<Sickness>();

		public PlayerScript playerScript;

		public event Action<DamageType> OnTakeDamageType;
		public event Action OnLowHealth;

		[SyncVar] public bool CannotRecognizeNames = false;


		public Dictionary<BodyPartType, ReagentMix> SurfaceReagents = new Dictionary<BodyPartType, ReagentMix>()
		{
			{BodyPartType.Head, new ReagentMix()},
			{BodyPartType.LeftArm, new ReagentMix()},
			{BodyPartType.RightArm, new ReagentMix()},
			{BodyPartType.LeftLeg, new ReagentMix()},
			{BodyPartType.RightLeg, new ReagentMix()},
			{BodyPartType.Chest, new ReagentMix()},
			//Maybe add feet for blood on boots?
		};


		public virtual void Awake()
		{
			rootBodyPartController = GetComponent<RootBodyPartController>();
			playerSprites = GetComponent<PlayerSprites>();
			BodyPartStorage = GetComponent<ItemStorage>();
			RegisterTile = GetComponent<RegisterTile>();
			RespiratorySystem = GetComponent<RespiratorySystemBase>();
			CirculatorySystem = GetComponent<CirculatorySystemBase>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			healthStateController = GetComponent<HealthStateController>();
			mobSickness = GetComponent<MobSickness>();
			playerScript = GetComponent<PlayerScript>();
			BodyPartStorage.ServerInventoryItemSlotSet += BodyPartTransfer;
		}


		//TODO: confusing, make it not depend from the inventory storage Action
		/// <summary>
		/// Server and client trigger this on both addition and removal of a bodypart
		/// </summary>
		private void BodyPartTransfer(Pickupable prevImplant, Pickupable newImplant)
		{
			if (newImplant && newImplant.TryGetComponent<BodyPart>(out var addedBodyPart))
			{
				addedBodyPart.BodyPartAddHealthMaster(this);
				SurfaceBodyParts.Add(addedBodyPart);
			}
			else if (prevImplant && prevImplant.TryGetComponent<BodyPart>(out var removedBodyPart))
			{
				removedBodyPart.BodyPartRemoveHealthMaster();
				if (SurfaceBodyParts.Contains(removedBodyPart))
				{
					SurfaceBodyParts.Remove(removedBodyPart);
				}
			}
		}

		public void BodyPartListChange()
		{
			CirculatorySystem.OrNull()?.BodyPartListChange();
			SurfaceBodyPartChanges();
		}

		public void SurfaceBodyPartChanges()
		{
			PrecalculatedMetabolismReactions.Clear();
			foreach (var externalReaction in allExternalMetabolismReactions)
			{
				foreach (var bodyPart in SurfaceBodyParts)
				{
					if (bodyPart.ItemAttributes.HasAllTraits(externalReaction.ExternalAllRequired) &&
					    bodyPart.ItemAttributes.HasAnyTrait(externalReaction.ExternalBlacklist) == false)
					{
						if (PrecalculatedMetabolismReactions.ContainsKey(externalReaction) == false)
						{
							PrecalculatedMetabolismReactions[externalReaction] = new List<BodyPart>();
						}

						PrecalculatedMetabolismReactions[externalReaction].Add(bodyPart);
					}
				}
			}
		}

		private List<BodyPart> TMPUseList = new List<BodyPart>();

		public void ExternalMetaboliseReactions()
		{
			var  node = RegisterTile.Matrix.MetaDataLayer.Get(transform.localPosition.RoundToInt());

			if (node != null && node.SmokeNode.IsActive)
			{
				if (RespiratorySystem == null || RespiratorySystem.IsEVACompatible() == false)
				{
					foreach (var SurfaceReagent in SurfaceReagents)
					{
						ApplyReagentsToSurface(node.SmokeNode.Present.Clone(), SurfaceReagent.Key);
					}
				}
			}

			if (node != null && node.FoamNode.IsActive)
			{
				if (RespiratorySystem == null || RespiratorySystem.IsEVACompatible() == false)
				{
					foreach (var SurfaceReagent in SurfaceReagents)
					{
						ApplyReagentsToSurface(node.SmokeNode.Present.Clone(), SurfaceReagent.Key);
					}
				}
			}

			foreach (var  storage in SurfaceReagents)
			{
				if (storage.Value.Total == 0) continue;

				MetabolismReactions.Clear();

				foreach (var Reaction in PrecalculatedMetabolismReactions)
				{
					var HasBodyPart = false;
					foreach (var bodyPart in PrecalculatedMetabolismReactions[Reaction.Key])
					{
						if (bodyPart.BodyPartType == storage.Key)
						{
							HasBodyPart = true;
							break;
						}
					}

					if (HasBodyPart)
					{
						Reaction.Key.Apply(this, storage.Value);
					}
				}

				foreach (var Reaction in MetabolismReactions)
				{
					TMPUseList.Clear();
					float ProcessingAmount = 0;
					foreach (var bodyPart in PrecalculatedMetabolismReactions[Reaction])
					{
						if (bodyPart.BodyPartType == storage.Key)
						{
							TMPUseList.Add(bodyPart);
							ProcessingAmount += 1;
						}
					}

					if (ProcessingAmount == 0) continue;

					Reaction.React(TMPUseList, storage.Value, ProcessingAmount);
				}

				storage.Value.Take(0.2f); //Evaporation
			}
		}

		[FormerlySerializedAs("AllExternalMetabolismReactions")] [FormerlySerializedAs("ALLExternalMetabolismReactions")] public List<ExternalBodyHealthEffect>
			allExternalMetabolismReactions = new List<ExternalBodyHealthEffect>(); //TOOD Move somewhere static maybe

		public List<MetabolismReaction> MetabolismReactions { get; } = new();

		private Dictionary<MetabolismReaction, List<BodyPart>> PrecalculatedMetabolismReactions =
			new Dictionary<MetabolismReaction, List<BodyPart>>();

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(PeriodicUpdate, 1f);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			StopCoroutine(ScreamCooldown());
		}

		public void Setbrain(Brain _brain)
		{
			brain = _brain;
		}

		[Server]
		public void SetMaxHealth(float newMaxHealth)
		{
			healthStateController.SetMaxHealth(newMaxHealth);
		}

		public override void OnStartServer()
		{
			mobID = PlayerManager.Instance.GetMobID();
			//Generate BloodType and DNA
			healthStateController.SetDNA(new DNAandBloodType());
		}

		public Reagent CHem;

		[RightClickMethod]
		public void InjectChemical()
		{
			CirculatorySystem.BloodPool.Add(CHem, 5);
		}

		[RightClickMethod]
		public void DODMG()
		{
			var bodyPart = BodyPartList.PickRandom();
			bodyPart.TakeDamage(null, 1, AttackType.Melee, DamageType.Brute);
		}

		public float NutrimentConsumed = 0;

		//Server Side only
		private void PeriodicUpdate()
		{
			NutrimentConsumed = 0;
			for (int i = BodyPartList.Count - 1; i >= 0; i--)
			{
				BodyPartList[i].ImplantPeriodicUpdate();
			}

			CirculatorySystem.BloodUpdate();
			ExternalMetaboliseReactions();

			FireStacksDamage();
			CalculateRadiationDamage();
			BleedStacksDamage();

			if (IsDead)
			{
				DeathPeriodicUpdate();
				return;
			}

			//Sickness logic should not be triggered if the player is dead.
			mobSickness.TriggerCustomSicknessLogic();

			CalculateOverallHealth();
		}

		/// <summary>
		/// Calculates and applies radiation damage
		/// </summary>
		[Server]
		public void CalculateRadiationDamage()
		{
			var radLevel = (RegisterTile.Matrix.GetRadiationLevel(RegisterTile.LocalPosition) * (tickRate / 5f));

			if (radLevel == 0) return;

			ApplyDamageAll(null, radLevel * 0.02f, AttackType.Rad, DamageType.Radiation);
		}

		/// <summary>Our
		/// Applys damage from fire stacks and handles their effects and decay
		/// </summary>
		public void FireStacksDamage()
		{
			if (fireStacks > 0)
			{
				//TODO: Burn clothes (see species.dm handle_fire)
				ApplyDamageAll(null, fireStacks, AttackType.Fire, DamageType.Burn, true);
				//gradually deplete fire stacks
				healthStateController.SetFireStacks(fireStacks - 0.1f);
				//instantly stop burning if there's no oxygen at this location
				MetaDataNode node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile.LocalPositionClient);
				if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
				{
					healthStateController.SetFireStacks(0);
					return;
				}

				RegisterTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition(), 700,
					true);
			}
		}

		/// <summary>
		/// Applies bleeding from bleedstacks and handles their effects.
		/// </summary>
		public void BleedStacksDamage()
		{
			if (BleedStacks > 0)
			{
				CirculatorySystem.Bleed(1f * (float) Math.Ceiling(BleedStacks));
				healthStateController.SetBleedStacks(BleedStacks - 0.1f);
			}
		}

		/// <summary>
		/// Returns the current amount of oxy damage the brain has taken
		/// </summary>
		public float GetOxyDamage => brain != null && brain.RelatedPart != null ? brain.RelatedPart.Oxy : 0;

		/// <summary>
		/// Returns the the sum of all brute damage taken by body parts
		/// </summary>
		public float GetTotalBruteDamage()
		{
			float toReturn = 0;
			foreach (var implant in BodyPartList)
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
			foreach (var implant in BodyPartList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				toReturn -= implant.Burn;
			}

			return toReturn;
		}

		/// <summary>
		/// Returns the the sum of all toxin damage taken by body parts
		/// </summary>
		public float GetTotalToxDamage()
		{
			float toReturn = 0;
			foreach (var implant in BodyPartList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				toReturn -= implant.Toxin;
			}

			return toReturn;
		}

		/// <summary>
		/// Returns the total amount of blood in the body of the type of blood the body should have
		/// </summary>
		public float GetTotalBlood()
		{
			return GetSpareBlood();
		}


		/// <summary>
		/// Returns the total amount of 'spare' blood outside of the organs
		/// </summary>
		public float GetSpareBlood()
		{
			return CirculatorySystem.BloodPool[CirculatorySystem.BloodType];
		}

		/// <summary>
		/// Returns true if the creature has the given body part of a type targetable by the UI
		/// </summary>
		/// <param name="bodyPartType">The type of Body Part to check</param>
		/// <param name="surfaceOnly">Checks only external bodyparts if true, all if false (default)</param>
		public bool HasBodyPart(BodyPartType bodyPartType, bool surfaceOnly = false)
		{
			foreach (var bodyPart in BodyPartList)
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
			float currentHealth = MaxHealth;
			foreach (var implant in BodyPartList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				currentHealth -= implant.TotalDamageWithoutOxyCloneRadStam;
			}

			if (brain == null || brain.RelatedPart.Health < -100)
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
			healthStateController.SetHunger(HungerState);
			healthStateController.SetBleedingState(BleedingState);

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
			foreach (var Implant in BodyPartList)
			{
				foreach (var organ in Implant.OrganList)
				{
					if (organ is Heart heart && heart.HeartAttack == false)
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
				var oldState = healthStateController.ConsciousState;
				if (isServer)
				{
					healthStateController.SetConsciousState(NewConsciousState);
					OnConsciousStateChangeServer.Invoke(oldState, NewConsciousState);
				}
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
		public void ApplyDamageAll(GameObject damagedBy, float damage, AttackType attackType, DamageType damageType,
			bool damageSplit = true)
		{
			if (damageSplit)
			{
				float bodyParts = SurfaceBodyParts.Count;
				damage /= bodyParts;
			}

			foreach (var bodyPart in SurfaceBodyParts.ToArray())
			{
				bodyPart.TakeDamage(damagedBy, damage, attackType, damageType, damageSplit);
			}

			if (damageType == DamageType.Brute)
			{
				//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
				//EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
			}

			IndicatePain(damage);
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
		public void ApplyDamageToBodyPart(GameObject damagedBy, float damage, AttackType attackType,
			DamageType damageType, BodyPartType bodyPartAim = BodyPartType.None, float armorPenetration = 0,
			double traumaDamageChance = 0, TraumaticDamageTypes tramuticDamageType = TraumaticDamageTypes.NONE)
		{
			if (bodyPartAim == BodyPartType.None)
			{
				bodyPartAim = BodyPartType.Chest.Randomize(0);
			}

			LastDamagedBy = damagedBy;

			var count = 0;

			// If targeting eyes or mouth, damage head instead
			if (bodyPartAim == BodyPartType.Eyes || bodyPartAim == BodyPartType.Mouth)
			{
				bodyPartAim = BodyPartType.Head;
			}

			//Currently there is no phyiscal "hand" or "foot" game object to be targeted.
			//We reasign these aims to the arms and legs instead.
			if (bodyPartAim == BodyPartType.LeftHand) bodyPartAim = BodyPartType.LeftArm;
			if (bodyPartAim == BodyPartType.RightHand) bodyPartAim = BodyPartType.RightArm;
			if (bodyPartAim == BodyPartType.LeftFoot) bodyPartAim = BodyPartType.LeftLeg;
			if (bodyPartAim == BodyPartType.RightFoot) bodyPartAim = BodyPartType.RightLeg;

			foreach (var bodyPart in SurfaceBodyParts)
			{
				if (bodyPart.BodyPartType == bodyPartAim)
				{
					count++;
				}
			}

			foreach (var bodyPart in SurfaceBodyParts.ToArray())
			{
				if (bodyPart.BodyPartType == bodyPartAim)
				{
					bodyPart.TakeDamage(damagedBy, damage / count, attackType, damageType,
						armorPenetration: armorPenetration,
						traumaDamageChance: traumaDamageChance, tramuticDamageType: tramuticDamageType);
				}
			}

			IndicatePain(damage);
			OnTakeDamageType?.Invoke(damageType);
			if (HealthIsLow()) OnLowHealth?.Invoke();
		}

		private bool HealthIsLow()
		{
			return HealthPercentage() < 35;
		}

		public float HealthPercentage()
		{
			return (OverallHealth / MaxHealth) * 100;
		}

		/// <summary>
		/// Only does damage to the first layer
		/// </summary>
		/// <returns></returns>
		public bool ZoneHasDamageOf(BodyPartType bodyPartAim, DamageType SpecifiedType)
		{
			foreach (var bodyPart in SurfaceBodyParts)
			{
				if (bodyPart.BodyPartType == bodyPartAim)
				{
					if (bodyPart.Damages[(int) SpecifiedType] > 0)
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Gets a list of all the stomachs in the creature
		/// </summary>
		/// <returns>List of Stomachs</returns>
		public List<Stomach> GetStomachs()
		{
			var Stomachs = new List<Stomach>();
			foreach (var Implant in BodyPartList)
			{
				foreach (var organ in Implant.OrganList)
				{
					var stomach = organ as Stomach;
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
			foreach (var bodyPart in BodyPartList)
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
			foreach (BodyPart bodyPart in BodyPartList)
			{
				if (bodyPart.BodyPartType == partType)
				{
					if (bodyPart.CurrentSlashDamageLevel > TraumaDamageLevel.NONE)
						return true;
					if (bodyPart.CurrentPierceDamageLevel > TraumaDamageLevel.NONE)
						return true;
					if (bodyPart.CurrentBurnDamageLevel > TraumaDamageLevel.NONE)
						return true;
					return bodyPart.CurrentBluntDamageLevel != TraumaDamageLevel.NONE;
				}
			}

			return false;
		}

		public void HealTraumaDamage(BodyPartType targetBodyPartToHeal, TraumaticDamageTypes typeToHeal)
		{
			foreach (var bodyPart in BodyPartList)
			{
				if (bodyPart.BodyPartType == targetBodyPartToHeal)
				{
					bodyPart.HealTraumaticDamage(typeToHeal);
				}
			}
		}

		/// <summary>
		/// Revives a dead player to full health.
		/// </summary>
		public void FullyHeal()
		{
			Extinguish(); //Remove any fire on them.
			ResetDamageAll(); //Bring their entire body parts that are on them in good shape.
			healthStateController
				.SetOverallHealth(MaxHealth); //Set the player's overall health to their race's maxHealth.
			RestartHeart();
			playerScript.playerMove.allowInput = true; //Let them interact with the world again.
			playerScript.registerTile.ServerStandUp();
			playerScript.ReturnGhostToBody();
		}

		public void RestartHeart()
		{
			foreach (var bodyPart in BodyPartList)
			{
				foreach (var organ in bodyPart.OrganList)
				{
					if (organ is Heart heart)
					{
						heart.HeartAttack = false;
						heart.CanTriggerHeartAttack = false;
						heart.CurrentPulse = 0;
					}
				}
			}

			CalculateOverallHealth(); //This makes the player alive and concision.
		}

		/// <summary>
		/// Apply healing to the creature. Server Only
		/// </summary>
		/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
		/// <param name="healAmt">Amount of healing to add</param>
		/// <param name="damageType">The Type of Damage To Heal</param>
		/// <param name="bodyPartAim">Body Part to heal</param>
		[Server]
		public void HealDamage(GameObject healingItem, float healAmt,
			DamageType damageTypeToHeal, BodyPartType bodyPartAim)
		{
			foreach (var bodyPart in SurfaceBodyParts)
			{
				if (bodyPart.BodyPartType == bodyPartAim)
				{
					bodyPart.HealDamage(healingItem, healAmt, damageTypeToHeal);
				}
			}
		}

		/// <summary>
		/// Apply healing to the creature on all body parts. Server Only
		/// </summary>
		/// <param name="healingItem">the item used for healing (bruise pack etc). Null if there is none</param>
		/// <param name="healAmt">Amount of healing to add</param>
		/// <param name="damageType">The Type of Damage To Heal</param>
		[Server]
		public void HealDamageOnAll(GameObject healingItem, float healAmt, DamageType damageTypeToHeal)
		{
			foreach (var bodyPart in SurfaceBodyParts)
			{
				bodyPart.HealDamage(healingItem, healAmt, damageTypeToHeal);
			}
		}

		[Server]
		public void ApplyReagentsToSurface(ReagentMix Chemicals, BodyPartType bodyPartAim) //is n(o) operation
		{
			foreach (var reaction in allExternalMetabolismReactions)
			{
				if (reaction.HasInitialTouchCharacteristics)
				{
					if (reaction.HasIngredients(Chemicals))
					{
						var Amount =  reaction.GetReactionAmount(Chemicals);
						foreach (var TouchCharacteristics in  reaction.InitialTouchCharacteristics)
						{
							ApplyDamageToBodyPart(this.gameObject, Amount * TouchCharacteristics.EffectPerOne,
								TouchCharacteristics.AttackType,
								TouchCharacteristics.DamageEffect, bodyPartAim);
						}
					}
				}
			}

			if (bodyPartAim == BodyPartType.None)
			{
				Chemicals.Divide(SurfaceReagents.Count);
				foreach (var surfaceReagent in SurfaceReagents)
				{
					AddToSurface(Chemicals, surfaceReagent.Value);
				}

				return;
			}

			if (SurfaceReagents.TryGetValue(bodyPartAim, out var mix) == false) return;

			AddToSurface(Chemicals, mix);
		}

		private void AddToSurface(ReagentMix Chemicals, ReagentMix mix)
		{
			mix.Add(Chemicals);

			if (mix.Total > BodyPartSurfaceVolume)
			{
				mix.Multiply(BodyPartSurfaceVolume / mix.Total);
			}
		}


		[Server]
		public virtual void OnGib()
		{
			_ = SoundManager.PlayAtPosition(CommonSounds.Instance.Slip, gameObject.transform.position,
				gameObject); //TODO: replace with gibbing noise
			CirculatorySystem.Bleed(GetTotalBlood());
			Death();
			for (int i = BodyPartList.Count - 1; i >= 0; i--)
			{
				BodyPartList[i].TryRemoveFromBody(true);
			}
		}

		public void DismemberBodyPart(BodyPart bodyPart)
		{
			bodyPart.TryRemoveFromBody();
		}

		///<Summary>
		/// Kills the creature, used for causes of death other than damage.
		///</Summary>
		public void Death()
		{
			//Don't trigger if already dead
			if (ConsciousState == ConsciousState.DEAD) return;

			timeOfDeath = GameManager.Instance.stationTime;

			var HV2 = (this as PlayerHealthV2);
			if (HV2 != null)
			{
				if (HV2.playerScript.OrNull()?.playerMove.OrNull()?.allowInput != null)
				{
					HV2.playerScript.playerMove.allowInput = false;
				}
			}

			SetConsciousState(ConsciousState.DEAD);
			OnDeathActions();
		}

		protected abstract void OnDeathActions();

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
			healthStateController.SetFireStacks(Mathf.Clamp((fireStacks + deltaValue), 0, maxFireStacks));
		}

		public void ChangeBleedStacks(float deltaValue)
		{
			healthStateController.SetBleedStacks(Mathf.Clamp((BleedStacks + deltaValue), 0, maxBleedStacks));
		}

		/// <summary>
		/// Removes all Fire Stacks
		/// </summary>
		public void Extinguish()
		{
			healthStateController.SetFireStacks(0);
		}

		private void DeathPeriodicUpdate()
		{
			MiasmaCreation();
		}

		private void MiasmaCreation()
		{
			//TODO:Check for non-organic/zombie/husk

			//Don't produce miasma until 2 minutes after death
			if (GameManager.Instance.stationTime.Subtract(timeOfDeath).TotalMinutes < 2) return;

			MetaDataNode node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile.LocalPositionClient);

			//Space or below -10 degrees celsius is safe from miasma creation
			if (node.IsSpace || node.GasMix.Temperature <= Reactions.KOffsetC - 10) return;

			//If we are in a container then don't produce miasma
			//TODO: make this only happen with coffins, body bags and other body containers (morgue, etc)
			if (objectBehaviour.ContainedInContainer != null) return;

			//TODO: check for formaldehyde in body, prevent if more than 15u

			//Don't continuously produce miasma, only produce max 4 moles on the tile
			if (node.GasMix.GetMoles(Gas.Miasma) > 4) return;

			node.GasMix.AddGas(Gas.Miasma, AtmosDefines.MIASMA_CORPSE_MOLES);
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

			var healthFraction = OverallHealth / MaxHealth;
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
				healthString.Append(
					$"<color=#b495bf>\n{theyPronoun} has a blank, absent-minded stare and appears completely unresponsive to anything. {theyPronoun} may snap out of it soon.</color>");
			}

			foreach (BodyPart part in BodyPartList)
			{
				if (part.IsSurface)
				{
					continue;
				}

				if (part.IsBleeding)
				{
					healthString.Append(
						$"<color=red>\n {theyPronoun} {part.BodyPartReadableName} is bleeding!</color>");
				}

				if (part.CurrentSlashDamageLevel >= TraumaDamageLevel.SERIOUS)
				{
					healthString.Append(
						$"<color=red>\n {theyPronoun} {part.BodyPartReadableName} is cut wide open!</color>");
				}

				if (part.CurrentPierceDamageLevel >= TraumaDamageLevel.SERIOUS)
				{
					healthString.Append(
						$"<color=red>\n {theyPronoun} have a huge hole in their {part.BodyPartReadableName}!</color>");
				}
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
			if (IsDead) return;

			foreach (var Race in RaceSOSingleton.Instance.Races)
			{
				if (Race.name == playerScript.characterSettings.Species)
				{
					if (sickness.ImmuneRaces.Contains(Race)) return;
					break;
				}
			}

			if ((mobSickness.HasSickness(sickness) == false) && (immunedSickness.Contains(sickness) == false))
				mobSickness.Add(sickness, Time.time);
			sickness.IsOnCooldown = false;
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
		protected LivingShockResponse GetElectrocutionSeverity(float shockPower)
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
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Sparks, electrocution.ShockSourcePos);

			float damage = shockPower;
			ApplyDamageAll(null, damage, AttackType.Internal, DamageType.Burn);
		}

		#endregion

		/// <summary>
		/// Sets up the sprite of a specified body part and adds its Net ID to InternalNetIDs
		/// </summary>
		/// <param name="implant">Body Part to display</param>
		public void ServerCreateSprite(BodyPart implant)
		{
			int i = 0;
			bool isSurfaceSprite = implant.IsSurface || implant.BodyPartItemInheritsSkinColor;
			var sprites = implant.GetBodyTypeSprites(playerSprites.ThisCharacter.BodyType);
			foreach (var Sprite in sprites.Item2)
			{
				var newSprite = Spawn
					.ServerPrefab(implant.SpritePrefab.gameObject, Vector3.zero, playerSprites.BodySprites.transform)
					.GameObject.GetComponent<BodyPartSprites>();
				newSprite.transform.localPosition = Vector3.zero;
				playerSprites.Addedbodypart.Add(newSprite);


				implant.RelatedPresentSprites.Add(newSprite);

				var newOrder = new SpriteOrder(sprites.Item1);
				newOrder.Add(i);

				var ClientData = new IntName();
				// TODO: names?? strings?? while using some sort of i at the same time??? WHAT IS THIS BURN IT
				ClientData.Name =
					implant.name + "_" + i + "_" + implant.GetInstanceID(); //is Fine because name is being Networked
				newSprite.SetName(ClientData.Name);
				ClientData.Int =
					CustomNetworkManager.Instance.IndexLookupSpawnablePrefabs[implant.SpritePrefab.gameObject];
				ClientData.Data = JsonConvert.SerializeObject(newOrder);
				implant.intName = ClientData;
				InternalNetIDs.Add(ClientData);

				newSprite.baseSpriteHandler.NetworkThis = true;
				newSprite.UpdateSpritesForImplant(implant, implant.ClothingHide, Sprite, newOrder);
				SpriteHandlerManager.RegisterHandler(playerSprites.GetComponent<NetworkIdentity>(),
					newSprite.baseSpriteHandler);

				if (isSurfaceSprite)
				{
					playerSprites.SurfaceSprite.Add(newSprite);
					HandleSurface(newSprite, implant);
				}


				i += 3; // ???????????????????????? for Sprite order clashes, for example hands not rendering over jumpsuit
			}

			rootBodyPartController.UpdateClients();

			if (implant.SetCustomisationData != "")
			{
				implant.LobbyCustomisation.OnPlayerBodyDeserialise(implant, implant.SetCustomisationData, this);
			}
		}


		public void HandleSurface(BodyPartSprites newSprite, BodyPart implant)
		{
			Color CurrentSurfaceColour = Color.white;
			if (implant.Tone == null) //Has no tone set
			{
				if (playerSprites.RaceBodyparts.Base.SkinColours.Count > 0)
				{
					ColorUtility.TryParseHtmlString(playerSprites.ThisCharacter.SkinTone, out CurrentSurfaceColour);

					var hasColour = false;

					foreach (var color in playerSprites.RaceBodyparts.Base.SkinColours)
					{
						if (color.ColorApprox(CurrentSurfaceColour))
						{
							hasColour = true;
							break;
						}
					}

					if (hasColour == false)
					{
						CurrentSurfaceColour = playerSprites.RaceBodyparts.Base.SkinColours[0];
					}
				}
				else
				{
					ColorUtility.TryParseHtmlString(playerSprites.ThisCharacter.SkinTone, out CurrentSurfaceColour);
				}
			}
			else //Already has tone set
			{
				CurrentSurfaceColour = implant.Tone.Value;
			}


			CurrentSurfaceColour.a = 1;
			newSprite.baseSpriteHandler.SetColor(CurrentSurfaceColour);
			implant.Tone = CurrentSurfaceColour;
			implant.BodyPartItemSprite.SetColor(CurrentSurfaceColour);
		}

		public List<BodyPartSprites> ClientSprites = new List<BodyPartSprites>();

		public void ClientUpdateSprites(List<IntName> NewInternalNetIDs)
		{
			List<SpriteHandler> SHS = new List<SpriteHandler>();

			//add new sprites
			foreach (var ID in NewInternalNetIDs)
			{
				bool Contains = false;
				foreach (var InetID in InternalNetIDs)
				{
					if (InetID.Name == ID.Name)
					{
						Contains = true;
					}
				}

				if (Contains == false)
				{
					if (CustomNetworkManager.Instance.allSpawnablePrefabs.Count > ID.Int)
					{
						var OB = Instantiate(CustomNetworkManager.Instance.allSpawnablePrefabs[ID.Int],
							playerSprites.BodySprites.transform).transform;
						var Net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(OB.gameObject);
						var Handlers = OB.GetComponentsInChildren<SpriteHandler>();

						foreach (var SH in Handlers)
						{
							SpriteHandlerManager.UnRegisterHandler(Net, SH);
						}

						OB.parent = playerSprites.BodySprites.transform;
						OB.localScale = Vector3.one;
						OB.localPosition = Vector3.zero;
						OB.localRotation = Quaternion.identity;

						var BPS = OB.GetComponent<BodyPartSprites>();
						BPS.SetName(ID.Name);
						ClientSprites.Add(BPS);
						if (playerSprites.Addedbodypart.Contains(BPS) == false)
						{
							playerSprites.Addedbodypart.Add(BPS);
						}

						foreach (var SH in Handlers)
						{
							SHS.Add(SH);
							SH.NetworkThis = true;
							SpriteHandlerManager.RegisterHandler(Net, SH);
						}
					}
				}
			}

			//removing sprites
			foreach (var ID in InternalNetIDs)
			{
				bool Contains = false;
				foreach (var InetID in NewInternalNetIDs)
				{
					if (InetID.Name == ID.Name)
					{
						Contains = true;
					}
				}

				if (Contains == false)
				{
					foreach (var bodyPartSprites in ClientSprites.ToArray())
					{
						if (bodyPartSprites.name == ID.Name)
						{
							if (playerSprites.Addedbodypart.Contains(bodyPartSprites))
							{
								playerSprites.Addedbodypart.Remove(bodyPartSprites);
							}

							ClientSprites.Remove(bodyPartSprites);

							var net = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(bodyPartSprites.gameObject);
							var handlers = bodyPartSprites.GetComponentsInChildren<SpriteHandler>();


							foreach (var handler in handlers)
							{
								SpriteHandlerManager.UnRegisterHandler(net, handler);
							}


							Destroy(bodyPartSprites.gameObject);
						}
					}
				}
			}

			foreach (var bodyPartSprites in ClientSprites)
			{
				foreach (var internalNetID in NewInternalNetIDs)
				{
					if (internalNetID.Name == bodyPartSprites.name)
					{
						bodyPartSprites.UpdateData(internalNetID.Data);
					}
				}
			}

			InternalNetIDs = NewInternalNetIDs;
		}

		public void IndicatePain(float dmgTaken)
		{
			if (EmoteActionManager.Instance == null || screamEmote == null ||
			    canScream == false || ConsciousState == ConsciousState.UNCONSCIOUS || IsDead) return;
			if (dmgTaken >= painScreamDamage) EmoteActionManager.DoEmote(screamEmote, playerScript.gameObject);
			StartCoroutine(ScreamCooldown());
		}

		private IEnumerator ScreamCooldown()
		{
			canScream = false;
			yield return WaitFor.Seconds(painScreamCooldown);
			canScream = true;
		}

		public void EnableFastRegen()
		{
			if (CustomNetworkManager.IsServer == false) return;
			UpdateManager.Add(FastRegen, tickRate);
		}

		private void FastRegen()
		{
			playerScript.registerTile.ServerRemoveStun();
			if(OverallHealth > fastRegenThreshold) return;

			HealDamageOnAll(null, fastRegenHeal, DamageType.Brute);
		}

		public void SetUpCharacter(PlayerHealthData RaceBodyparts)
		{
			if (CustomNetworkManager.Instance._isServer)
			{
				InstantiateAndSetUp(RaceBodyparts.Base.Head);
				InstantiateAndSetUp(RaceBodyparts.Base.Torso);
				InstantiateAndSetUp(RaceBodyparts.Base.ArmLeft);
				InstantiateAndSetUp(RaceBodyparts.Base.ArmRight);
				InstantiateAndSetUp(RaceBodyparts.Base.LegLeft);
				InstantiateAndSetUp(RaceBodyparts.Base.LegRight);
			}
		}

		public void InitialiseFromRaceData(PlayerHealthData RaceBodyparts)
		{
			CirculatorySystem.SetBloodType(RaceBodyparts.Base.BloodType);
			CirculatorySystem.InitialiseHunger(RaceBodyparts.Base.NumberOfMinutesBeforeStarving);
			CirculatorySystem.InitialiseToxGeneration(RaceBodyparts.Base.TotalToxinGenerationPerSecond);
			CirculatorySystem.InitialiseMetabolism(RaceBodyparts);
			CirculatorySystem.InitialiseDefaults(RaceBodyparts);
			CirculatorySystem.BodyPartListChange();
		}

		public void InstantiateAndSetUp(ObjectList ListToSpawn)
		{
			if (ListToSpawn != null && ListToSpawn.Elements.Count > 0)
			{
				foreach (var ToSpawn in ListToSpawn.Elements)
				{
					var bodyPartObject = Spawn.ServerPrefab(ToSpawn).GameObject;
					BodyPartStorage.ServerTryAdd(bodyPartObject);
				}
			}
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) ||
			    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold) == false)
			{
				return null;
			}

			return RightClickableResult.Create()
				.AddAdminElement("Heal", AdminSmash);
		}

		private void AdminSmash()
		{
			AdminCommandsManager.Instance.CmdHealMob(gameObject);
		}
	}

	/// <summary>
	/// Event which fires when fire stack value changes.
	/// </summary>
	public class FireStackEvent : UnityEvent<float>
	{
	}

	/// <summary>
	/// Event which fires when conscious state changes, provides the old state and the new state
	/// </summary>
	public class ConsciousStateEvent : UnityEvent<ConsciousState, ConsciousState>
	{
	}
}