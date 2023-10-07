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
using Core.Utils;
using Health.Sickness;
using HealthV2.Living.CirculatorySystem;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Items.Implants.Organs;
using JetBrains.Annotations;
using Logs;
using NaughtyAttributes;
using Player;
using Newtonsoft.Json;
using ScriptableObjects.RP;
using Systems.Construction.Parts;
using Systems.Score;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;
using Systems.Character;

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
		IAreaReactionBase, IRightClickable, IServerSpawn, IHoverTooltip, IChargeable
	{
		public bool DoesNotRequireBrain = false;

		/// <summary>
		/// Server side, each mob has a different one and never it never changes
		/// </summary>
		public int mobID { get; private set; }

		// TODO: Add a way to change tickRates based on specific conditions such as players controlling specific mobs that require the tickrate to change to a different value
		/// <summary>
		/// Rate at which periodic damage, such as radiation, should be applied
		/// </summary>
		[SerializeField] private float tickRate = 1f;

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

		public ReagentPoolSystem reagentPoolSystem => ActiveSystems.OfType<ReagentPoolSystem>().FirstOrDefault();

		///<summary>
		/// Fetch first or default system by type from the active systems on this living thing.
		///</summary>
		public T GetSystem<T>()
		{
			return ActiveSystems.OfType<T>().FirstOrDefault();
		}

		public bool TryGetSystem<T>(out T system)
		{
			system = ActiveSystems.OfType<T>().FirstOrDefault();
			return Equals(system, default(T));
		}
		
		public Brain brain { get; private set; }


		[SyncVar(hook = nameof(SyncBain))] private uint BrainID;

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
		private float fireStacks;

		private bool HasFireStacksCash = false;

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
		public float BleedStacks;

		private float maxBleedStacks = 10f;

		[SerializeField, BoxGroup("PainFeedback")]
		private float painScreamDamage = 20f;

		public float PainScreamDamage => painScreamDamage;

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


		public ChatModifier BodyChatModifier = ChatModifier.None;

		public float BodyPartSurfaceVolume = 5;

		public List<HealthSystemBase> ActiveSystems = new List<HealthSystemBase>();


		private BodyAlertManager BodyAlertManager;


		public BleedingState BleedingState => CalculateBleedingState();

		private BleedingState CashedBleedingState;

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

		public event Action<DamageType, GameObject, float> OnTakeDamageType;
		public event Action OnLowHealth;

		public event Action OnDeath;
		public UnityEvent OnRevive;
		public UnityEvent OnCrit;
		public UnityEvent OnCritExit;

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

		public BodyPartType[] UsedZones = new BodyPartType[]
		{
			BodyPartType.Head,
			BodyPartType.Chest,
			BodyPartType.LeftArm,
			BodyPartType.RightArm,
			BodyPartType.LeftLeg,
			BodyPartType.RightLeg
		};

		[SerializeField] private GameObject meatProduce;
		[SerializeField] private GameObject skinProduce;
		public GameObject MeatProduce => meatProduce;
		public GameObject SkinProduce => skinProduce;


		[NonSerialized] public MultiInterestBool IsMute = new MultiInterestBool(true,
			MultiInterestBool.RegisterBehaviour.RegisterFalse,
			MultiInterestBool.BoolBehaviour.ReturnOnTrue);

		[SerializeField, Range(1, 60f)] private float updateTime = 1f;

		[NonSerialized] public PlayerHealthData InitialSpecies = null;

		//Default is mute yes

		public bool HasCoreBodyPart()
		{
			foreach (var BodyPart in SurfaceBodyParts)
			{
				if (BodyPart.ItemAttributes.HasTrait(CommonTraits.Instance.CoreBodyPart))
				{
					return true;
				}
			}

			return false;
		}

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
			BodyPartStorage.SetRegisterPlayer(GetComponent<RegisterPlayer>());
			BodyAlertManager = GetComponent<BodyAlertManager>();
			//Needs to be in awake so the mobId is set before mind transfer (OnSpawnServer happens after that so cannot be used)
			mobID = PlayerManager.Instance.GetMobID();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//Generate BloodType and DNA
			healthStateController.SetDNA(new DNAandBloodType());

			if (playerScript == null) return;
			if (playerScript.Mind?.occupation?.DisplayName == "Clown")
			{
				OnTakeDamageType += ClownAbuseScoreEvent;
			}
		}

		public void SetBrain(Brain newBrain)
		{
			brain = newBrain;
			if (newBrain == null)
			{
			}
			else
			{
				BrainID = newBrain.gameObject.NetId();
			}
		}

		private void SyncBain(uint oldId, uint newID)
		{
			BrainID = newID;
			if (newID is NetId.Empty or NetId.Invalid)
			{
				brain = null;
				return;
			}

			var spawnedList = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;


			if (spawnedList.ContainsKey(newID))
			{
				brain = spawnedList[newID].GetComponent<Brain>();
			}
		}


		public bool IsFullyCharged
		{
			get
			{
				var chargeable = GetSystem<BatterySystem>();
				if (chargeable == null)
				{
					return true;
				}
				else
				{
					return chargeable.IsFullyCharged;
				}
			}
		}

		public void ChargeBy(float watts)
		{
			var chargeable = GetSystem<BatterySystem>();
			if (chargeable == null)
			{
				return;
			}
			else
			{
				chargeable.ChargeBy(watts);
			}
		}

		//TODO: confusing, make it not depend from the inventory storage Action
		/// <summary>
		/// Server and client trigger this on both addition and removal of a bodypart
		/// </summary>
		private void BodyPartTransfer(Pickupable prevImplant, Pickupable newImplant)
		{
			//SO
			//Body part with transparency added or removed
			//Body parts removed from transparent Body part

			if (newImplant && newImplant.TryGetComponent<BodyPart>(out var addedBodyPart))
			{
				addedBodyPart.BodyPartAddHealthMaster(this); //Don't worry It comes back around
			}
			else if (prevImplant && prevImplant.TryGetComponent<BodyPart>(out var removedBodyPart))
			{
				removedBodyPart.BodyPartRemoveHealthMaster(); //Don't worry It comes back around
			}


			if (prevImplant && newImplant == null)
			{
				if (BodyPartStorage.HasAnyOccupied() == false)
				{
					//TODO cyborg chassis doesn't appear, shouldn't be able to get a  chassis by itself?
					_ = Despawn.ServerSingle(this.gameObject);
				}
			}
		}

		public void AddingBodyPart(BodyPart BodyPart)
		{
			if (BodyPartList.Contains(BodyPart) == false)
			{
				BodyPartList.Add(BodyPart);
			}

			if (BodyPart.IsInAnOpenAir)
			{
				if (SurfaceBodyParts.Contains(BodyPart) == false)
				{
					SurfaceBodyParts.Add(BodyPart);
				}
			}


			foreach (var sys in ActiveSystems)
			{
				sys.BodyPartAdded(BodyPart);
			}
		}

		public void RemovingBodyPart(BodyPart BodyPart)
		{
			if (BodyPartList.Contains(BodyPart))
			{
				BodyPartList.Remove(BodyPart);
			}

			if (SurfaceBodyParts.Contains(BodyPart))
			{
				SurfaceBodyParts.Remove(BodyPart);
			}
		}

		public void BodyPartListChange()
		{
			SurfaceBodyPartChanges();
			BodyPartsChangeMutation();
		}

		public void SurfaceBodyPartChanges()
		{
			PrecalculatedMetabolismReactions.Clear();
			foreach (var externalReaction in allExternalMetabolismReactions)
			{
				foreach (var bodyPart in SurfaceBodyParts)
				{
					if (bodyPart.ItemAttributes.HasAllTraits(externalReaction.ExternalAllRequired)
					    && bodyPart.ItemAttributes.HasAnyTrait(externalReaction.ExternalBlacklist) == false
					    && bodyPart.TryGetComponent<MetabolismComponent>(out var MetabolismComponent))
					{
						if (PrecalculatedMetabolismReactions.ContainsKey(externalReaction) == false)
						{
							PrecalculatedMetabolismReactions[externalReaction] = new List<MetabolismComponent>();
						}

						PrecalculatedMetabolismReactions[externalReaction].Add(MetabolismComponent);
					}
				}
			}
		}


		private List<MetabolismComponent> TMPUseList = new List<MetabolismComponent>();

		public void ExternalMetaboliseReactions()
		{
			var node = RegisterTile.Matrix.MetaDataLayer.Get(transform.localPosition.RoundToInt());

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

			foreach (var storage in SurfaceReagents)
			{
				if (storage.Value.Total == 0) continue;

				MetabolismReactions.Clear();

				foreach (var Reaction in PrecalculatedMetabolismReactions)
				{
					var HasBodyPart = false;
					foreach (var bodyPart in PrecalculatedMetabolismReactions[Reaction.Key])
					{
						if (SurfaceReagents.ContainsKey(bodyPart.RelatedPart.BodyPartType) == false)
						{
							if (BodyPartType.Chest == storage.Key)
							{
								HasBodyPart = true;
								break;
							}
						}
						else
						{
							if (bodyPart.RelatedPart.BodyPartType == storage.Key)
							{
								HasBodyPart = true;
								break;
							}
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
						if (SurfaceReagents.ContainsKey(bodyPart.RelatedPart.BodyPartType) == false)
						{
							if (BodyPartType.Chest == storage.Key)
							{
								TMPUseList.Add(bodyPart);
								ProcessingAmount += 1;
							}
						}
						else
						{
							if (bodyPart.RelatedPart.BodyPartType == storage.Key)
							{
								TMPUseList.Add(bodyPart);
								ProcessingAmount += 1;
							}
						}
					}

					if (ProcessingAmount == 0) continue;

					Reaction.React(TMPUseList, storage.Value, ProcessingAmount);
				}

				storage.Value.Take(0.2f); //Evaporation
			}
		}

		[FormerlySerializedAs("AllExternalMetabolismReactions")]
		[FormerlySerializedAs("ALLExternalMetabolismReactions")]
		public List<ExternalBodyHealthEffect>
			allExternalMetabolismReactions = new List<ExternalBodyHealthEffect>(); //TOOD Move somewhere static maybe

		public List<MetabolismReaction> MetabolismReactions { get; } = new();

		private Dictionary<MetabolismReaction, List<MetabolismComponent>> PrecalculatedMetabolismReactions =
			new Dictionary<MetabolismReaction, List<MetabolismComponent>>();

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(PeriodicUpdate, updateTime);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			StopCoroutine(ScreamCooldown());
		}

		[Server]
		public void SetMaxHealth(float newMaxHealth)
		{
			healthStateController.SetMaxHealth(newMaxHealth);
		}


		[RightClickMethod]
		public void DODMG()
		{
			var bodyPart = BodyPartList.PickRandom();
			bodyPart.TakeDamage(null, 1, AttackType.Melee, DamageType.Brute);
		}

		public float NutrimentConsumed = 0;
		[SerializeField] private bool stopOverallCalculation = false;
		[SerializeField] private bool stopHealthSystems = false;

		//Server Side only
		private void PeriodicUpdate()
		{
			NutrimentConsumed = 0;
			for (int i = BodyPartList.Count - 1; i >= 0; i--)
			{
				if (stopHealthSystems == true)
					continue;
				BodyPartList[i].ImplantPeriodicUpdate();
			}


			foreach (var system in ActiveSystems)
			{
				if (stopHealthSystems == true)
					continue;
				system.SystemUpdate();
			}


			if (stopHealthSystems == false)
				ExternalMetaboliseReactions();

			FireStacksDamage();
			CalculateRadiationDamage();
			BleedStacksDamage();

			EnvironmentDamage();

			if (!stopOverallCalculation)
				CalculateOverallHealth();


			if (IsDead)
			{
				DeathPeriodicUpdate();
				return;
			}

			//Sickness logic should not be triggered if the player is dead.
			mobSickness.TriggerCustomSicknessLogic();
		}

		#region Mutations

		public int Stability = 0;
		public int NegativeMutationMinimumTimeMinutes = 1;
		public int NegativeMutationMaximumTimeMinutes = 4;
		private Coroutine routine;


		public void BodyPartsChangeMutation()
		{
			Stability = 0;
			foreach (var BP in BodyPartList)
			{
				var Mutation = BP.CommonComponents.SafeGetComponent<BodyPartMutations>();
				if (Mutation != null)
				{
					Stability += Mutation.Stability;
				}
			}

			if (routine == null)
			{
				if (Stability < 0)
				{
					routine = StartCoroutine(ApplyNegativeMutation());
				}
			}
		}


		private IEnumerator ApplyNegativeMutation()
		{
			List<BodyPartMutations.MutationAndBodyPart> AvailableMutations = new
				List<BodyPartMutations.MutationAndBodyPart>();
			while (Stability < 0)
			{
				yield return WaitFor.Minutes(Random.Range(NegativeMutationMinimumTimeMinutes,
					NegativeMutationMaximumTimeMinutes));
				AvailableMutations.Clear();

				foreach (var BP in BodyPartList)
				{
					var Mutation = BP.CommonComponents.SafeGetComponent<BodyPartMutations>();
					if (Mutation != null)
					{
						Mutation.GetAvailableNegativeMutations(AvailableMutations);
					}
				}

				if (AvailableMutations.Count == 0)
				{
					routine = null;
					yield break;
				}

				var MutationToApply = AvailableMutations.PickRandom();

				MutationToApply.BodyPartMutations.AddMutation(MutationToApply.MutationSO);
			}

			routine = null;
		}


		public Coroutine InjectDna(List<DNAMutationData> Payloads, bool skipWaiting = false,
			CharacterSheet characterSheet = null)
		{
			return StartCoroutine(EnumeratorInjectDna(Payloads, skipWaiting, characterSheet));
		}


		private IEnumerator EnumeratorInjectDna(List<DNAMutationData> Payloads, bool skipWaiting = false,
			CharacterSheet characterSheet = null)
		{
			foreach (var Payload in Payloads)
			{
				yield return StartCoroutine(ProcessDnaPayload(Payload, skipWaiting, characterSheet));
			}
		}

		public IEnumerator ProcessDnaPayload(DNAMutationData InDNAMutationData, bool skipWaiting = false,
			CharacterSheet characterSheet = null)
		{
			//TODO Skin and body type , is in character Settings  so is awkward
			foreach (var Payload in InDNAMutationData.Payload)
			{
				if (skipWaiting == false)
					yield return WaitFor.Seconds(1f);
				foreach (var BP in BodyPartList)
				{
					if (BP.name.ToLower().Contains(InDNAMutationData.BodyPartSearchString.ToLower()) == false) continue;
					var Mutation = BP.GetComponent<BodyPartMutations>();
					if (Mutation == null) continue;
					if (skipWaiting == false)
						yield return WaitFor.Seconds(1f);
					if (string.IsNullOrEmpty(Payload.CustomisationTarget) == false ||
					    string.IsNullOrEmpty(Payload.CustomisationReplaceWith) == false)
					{
						Mutation.MutateCustomisation(Payload.CustomisationTarget,
							Payload.CustomisationReplaceWith);
					}

					if (Payload.RemoveTargetMutationSO != null)
					{
						Mutation.RemoveMutation(Payload.RemoveTargetMutationSO);
					}

					if (Payload.TargetMutationSO != null)
					{
						Mutation.AddMutation(Payload.TargetMutationSO);
					}

					if (Payload.SpeciesMutateTo != null && Payload.MutateToBodyPart != null)
					{
						Mutation.ChangeToSpecies(Payload.MutateToBodyPart, characterSheet);
						// Anyway triggers error because changed BodyPartList
						break;
					}
				}
			}
		}

		public void RefreshPumps()
		{
			reagentPoolSystem.RefreshPumps(BodyPartList);
		}

		/// <summary>
		/// Updates blood pool with giving body start blood and removing previous
		/// </summary>
		public void UpdateBloodPool(bool needToTransferFood = false)
		{
			reagentPoolSystem.UpdateBloodPool(needToTransferFood, GetSystem<HungerSystem>());
		}

		/// <summary>
		/// Updates meat Produce and skin through getting them from character sheet
		/// </summary>
		public void UpdateMeatAndSkinProduce()
		{
			var raceParts = playerSprites.ThisCharacter.GetRaceSoNoValidation();
			meatProduce = raceParts.Base.MeatProduce;
			skinProduce = raceParts.Base.SkinProduce;
		}

		#endregion

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
			if (HasFireStacksCash != fireStacks > 0)
			{
				HasFireStacksCash = fireStacks > 0;

				if (HasFireStacksCash)
				{
					BodyAlertManager.RegisterAlert(CommonAlertSOs.Instance.HasFireStacks);
				}
				else
				{
					BodyAlertManager.UnRegisterAlert(CommonAlertSOs.Instance.HasFireStacks);
				}
			}

			if (fireStacks <= 0) return;
			//TODO: Burn clothes (see species.dm handle_fire)
			ApplyDamageAll(null, fireStacks, AttackType.Fire, DamageType.Burn, true, TraumaticDamageTypes.BURN);
			//gradually deplete fire stacks
			fireStacks -= 0.1f;
			//instantly stop burning if there's no oxygen at this location
			MetaDataNode
				node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile
					.LocalPositionClient); //TODO Account for containers
			if (node.GasMix.GetMoles(Gas.Oxygen) < 1)
			{
				fireStacks = 0;
				return;
			}

			RegisterTile.Matrix.ReactionManager.ExposeHotspotWorldPosition(gameObject.TileWorldPosition(), 700,
				true);
		}

		/// <summary>
		/// Applies bleeding from bleedstacks and handles their effects.
		/// </summary>
		public void BleedStacksDamage()
		{
			if (BleedStacks > 0)
			{
				reagentPoolSystem?.Bleed(1f * (float) Math.Ceiling(BleedStacks));
				BleedStacks = BleedStacks - 0.1f;
			}
		}

		public void EnvironmentDamage()
		{
			var ambientGasMix = GasMix.GetEnvironmentalGasMixForObject(this.objectBehaviour);

			ExposePressureTemperature(ambientGasMix.Pressure, ambientGasMix.Temperature);
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

		public AlertSO GetAlertSOFromBleedingState(BleedingState HungerStates)
		{
			switch (HungerStates)
			{
				case BleedingState.UhOh:
					return CommonAlertSOs.Instance.Bleeding_UhOh;
				case BleedingState.High:
					return CommonAlertSOs.Instance.Bleeding_High;
				case BleedingState.Medium:
					return CommonAlertSOs.Instance.Bleeding_Medium;
				case BleedingState.Low:
					return CommonAlertSOs.Instance.Bleeding_Low;
				case BleedingState.VeryLow:
					return CommonAlertSOs.Instance.Bleeding_VeryLow;
				default:
					return null;
			}
		}


		/// <summary>
		/// Updates overall health based on damage sustained by body parts thus far.
		/// Also updates consciousness status and will initiate a heart attack if low enough.
		/// </summary>
		public void CalculateOverallHealth()
		{
			float currentHealth = MaxHealth;
			var conState = ConsciousState;
			foreach (var implant in BodyPartList)
			{
				if (implant.DamageContributesToOverallHealth == false) continue;
				currentHealth -= implant.TotalDamageWithoutOxyCloneRadStam;
			}

			if (DoesNotRequireBrain == false)
			{
				if (brain == null || brain.RelatedPart.Health < -100 || brain.RelatedPart.TotalModified == 0)
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
			}


			//Sync health
			healthStateController.SetOverallHealth(currentHealth);

			//TODO HungerState should properly have a cash optimisation here!!
			if (BleedingState != CashedBleedingState)
			{
				var old = GetAlertSOFromBleedingState(CashedBleedingState);
				if (old != null)
				{
					BodyAlertManager.UnRegisterAlert(old);
				}

				CashedBleedingState = BleedingState;

				var newOne = GetAlertSOFromBleedingState(BleedingState);
				if (newOne != null)
				{
					BodyAlertManager.RegisterAlert(newOne);
				}
			}

			if (currentHealth < -100)
			{
				CheckHeartStatus();
				OnCrit?.Invoke();
			}
			else if (currentHealth < -50)
			{
				SetConsciousState(ConsciousState.UNCONSCIOUS);
				OnCrit?.Invoke();
			}
			else if (currentHealth < 0)
			{
				SetConsciousState(ConsciousState.BARELY_CONSCIOUS);
				OnCrit?.Invoke();
			}
			else
			{
				SetConsciousState(ConsciousState.CONSCIOUS);
			}

			if (conState == ConsciousState.UNCONSCIOUS &&
			    ConsciousState is ConsciousState.CONSCIOUS or ConsciousState.BARELY_CONSCIOUS)
			{
				OnCritExit?.Invoke();
			}

			if (conState == ConsciousState.DEAD &&
			    ConsciousState is ConsciousState.CONSCIOUS or ConsciousState.BARELY_CONSCIOUS)
			{
				OnRevive?.Invoke();
			}
		}

		private void CheckHeartStatus()
		{
			bool hasAllHeartAttack = BodyPartList.Count != 0;
			foreach (var Implant in BodyPartList)
			{
				foreach (var organ in Implant.OrganList)
				{
					if (organ is Heart heart && heart.HeartAttack == false && heart.CanHaveHeartAttack)
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

		public void SetConsciousState(ConsciousState NewConsciousState)
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
			bool damageSplit = true, TraumaticDamageTypes traumaticDamageTypes = TraumaticDamageTypes.NONE,
			double traumaChance = 50)
		{
			if (damageSplit)
			{
				float bodyParts = SurfaceBodyParts.Count;
				damage /= bodyParts;
			}

			foreach (var bodyPart in SurfaceBodyParts.ToArray())
			{
				bodyPart.TakeDamage(damagedBy, damage, attackType, damageType, damageSplit,
					default, default, traumaChance, traumaticDamageTypes);
			}

			if (damageType == DamageType.Brute)
			{
				//TODO: Re - impliment this using the new reagent- first code introduced in PR #6810
				//EffectsFactory.BloodSplat(RegisterTile.WorldPositionServer, BloodSplatSize.large, BloodSplatType.red);
			}

			IndicatePain(damage);
			OnTakeDamageType?.Invoke(damageType, damagedBy, damage);
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
			bodyPartAim = AimToUsableAim(bodyPartAim);

			LastDamagedBy = damagedBy;

			var listHitting = GetBodyPartsInArea(bodyPartAim, false);
			var count = listHitting.Count;
			foreach (var bodyPart in listHitting)
			{
				bodyPart.TakeDamage(damagedBy, damage / count, attackType, damageType,
					armorPenetration: armorPenetration,
					traumaDamageChance: traumaDamageChance, tramuticDamageType: tramuticDamageType);
			}

			IndicatePain(damage);
			OnTakeDamageType?.Invoke(damageType, damagedBy, damage);
			if (HealthIsLow()) OnLowHealth?.Invoke();
		}


		public BodyPartType AimToUsableAim(BodyPartType bodyPartAim)
		{
			if (bodyPartAim == BodyPartType.None)
			{
				bodyPartAim = BodyPartType.Chest.Randomize(0);
			}

			//Currently there is no phyiscal "hand" or "foot" game object to be targeted.
			//We reasign these aims to the arms and legs instead.
			if (bodyPartAim == BodyPartType.LeftHand) bodyPartAim = BodyPartType.LeftArm;
			if (bodyPartAim == BodyPartType.RightHand) bodyPartAim = BodyPartType.RightArm;
			if (bodyPartAim == BodyPartType.LeftFoot) bodyPartAim = BodyPartType.LeftLeg;
			if (bodyPartAim == BodyPartType.RightFoot) bodyPartAim = BodyPartType.RightLeg;
			return bodyPartAim;
		}

		public BodyPart GetFirstBodyPartInArea(BodyPartType bodyPartAim, bool validateAim = true)
		{
			if (validateAim)
			{
				bodyPartAim = AimToUsableAim(bodyPartAim);
			}

			switch (bodyPartAim)
			{
				case BodyPartType.Eyes:
				case BodyPartType.Mouth:
					foreach (var bodyPart in BodyPartList)
					{
						if (bodyPart.BodyPartType == bodyPartAim)
						{
							return bodyPart;
						}
					}

					break;
				default:
					foreach (var bodyPart in SurfaceBodyParts)
					{
						if (bodyPart.BodyPartType == bodyPartAim)
						{
							return bodyPart;
						}
					}

					break;
			}

			return null;
		}

		public List<BodyPart> GetBodyPartsInArea(BodyPartType bodyPartAim, bool validateAim = true)
		{
			var ReturnList = new List<BodyPart>();
			if (validateAim)
			{
				bodyPartAim = AimToUsableAim(bodyPartAim);
			}

			switch (bodyPartAim)
			{
				case BodyPartType.Eyes:
				case BodyPartType.Mouth:
					foreach (var bodyPart in BodyPartList)
					{
						if (bodyPart.BodyPartType == bodyPartAim)
						{
							ReturnList.Add(bodyPart);
						}
					}

					break;
				default:
					foreach (var bodyPart in SurfaceBodyParts)
					{
						if (bodyPart.BodyPartType == bodyPartAim)
						{
							ReturnList.Add(bodyPart);
						}
					}

					break;
			}

			return ReturnList;
		}

		public bool TryFlash(float flashDuration, bool checkForProtectiveCloth = true)
		{
			bool didFlash = false;
			var eyes = GetBodyPartsInArea(BodyPartType.Eyes, false);
			foreach (var eye in eyes)
			{
				var EyeFlash = eye.GetComponentCustom<EyeFlash>();
				if (EyeFlash != null && EyeFlash.TryFlash(flashDuration, checkForProtectiveCloth))
				{
					didFlash = true;
				}
			}

			return didFlash;
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
		/// Revives a dead player to full health.
		/// </summary>
		public void FullyHeal()
		{
			Extinguish(); //Remove any fire on them.
			ResetDamageAll(); //Bring their entire body parts that are on them in good shape.
			healthStateController
				.SetOverallHealth(MaxHealth); //Set the player's overall health to their race's maxHealth.
			RestartHeart();
			SetConsciousState(ConsciousState.CONSCIOUS);
			playerScript.RegisterPlayer.ServerStandUp();
			playerScript.Mind.OrNull()?.StopGhosting();
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

		public void StopOverralCalculation()
		{
			stopOverallCalculation = true;
		}

		public void UnstopOverallCalculation()
		{
			stopOverallCalculation = false;
		}

		public void StopHealthSystemsAndHeart()
		{
			foreach (var bodyPart in BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Heart heart)
					{
						heart.HeartAttack = true;
						heart.CanTriggerHeartAttack = false;
						heart.CurrentPulse = 0;
						continue;
					}

					if (organ is Eye eye)
					{
						eye.BadEyesight = 10;
					}
				}
			}

			CalculateOverallHealth();
			stopHealthSystems = true;
		}

		public void UnstopHealthSystemsAndRestartHeart()
		{
			stopHealthSystems = false;
			foreach (var bodyPart in BodyPartList)
			{
				foreach (BodyPartFunctionality organ in bodyPart.OrganList)
				{
					if (organ is Eye eye)
					{
						eye.BadEyesight = 0;
					}
				}
			}

			RestartHeart();
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
			DamageType damageTypeToHeal, BodyPartType bodyPartAim, bool ExternalHealing = false)
		{
			foreach (var bodyPart in SurfaceBodyParts)
			{
				if (bodyPart.BodyPartType == bodyPartAim)
				{
					if (ExternalHealing && bodyPart.CanNotBeHealedByExternalHealingPack)
					{
						continue;
					}

					bodyPart.HealDamage(healingItem, healAmt, damageTypeToHeal);
				}
			}

			ScoreMachine.AddToScoreInt((int) healAmt, RoundEndScoreBuilder.COMMON_SCORE_HEALING);
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
						var Amount = reaction.GetReactionAmount(Chemicals);
						foreach (var TouchCharacteristics in reaction.InitialTouchCharacteristics)
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

			reagentPoolSystem?.Bleed(reagentPoolSystem.GetTotalBlood());

			Death();
			for (int i = BodyPartList.Count - 1; i >= 0; i--)
			{
				if (BodyPartList[i].BodyPartType == BodyPartType.Chest) continue;
				BodyPartList[i].TryRemoveFromBody(true, PreventGibb_Death: true);
			}
		}

		public void DismemberBodyPart(BodyPart bodyPart)
		{
			bodyPart.TryRemoveFromBody();
		}

		///<Summary>
		/// Kills the creature, used for causes of death other than damage.
		///</Summary>
		public void Death(bool invokeDeathEvent = true)
		{
			//Don't trigger if already dead
			if (ConsciousState == ConsciousState.DEAD) return;

			timeOfDeath = GameManager.Instance.RoundTime;

			SetConsciousState(ConsciousState.DEAD);
			OnDeathActions();
			if (invokeDeathEvent) OnDeath?.Invoke();
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
			fireStacks = Mathf.Clamp((fireStacks + deltaValue), 0, maxFireStacks);
		}

		/// <summary>
		/// Adds a number of bleed stacks on the creature. Use negative numbers to reduce the number of stacks.
		/// </summary>
		public void ChangeBleedStacks(float deltaValue)
		{
			BleedStacks = Mathf.Clamp((BleedStacks + deltaValue), 0, maxBleedStacks);
		}

		/// <summary>
		/// Forces a number of bleed stacks on the creature.
		/// </summary>
		public void SetBleedStacks(float deltaValue)
		{
			BleedStacks = (Mathf.Clamp(deltaValue, 0, maxBleedStacks));
		}

		/// <summary>
		/// Removes all Fire Stacks
		/// </summary>
		public void Extinguish()
		{
			fireStacks = 0;
		}

		private void DeathPeriodicUpdate()
		{
			MiasmaCreation();
		}

		private void MiasmaCreation()
		{
			//TODO:Check for non-organic/zombie/husk

			//Don't produce miasma until 2 minutes after death
			if (GameManager.Instance.RoundTime.Subtract(timeOfDeath).TotalMinutes < 2) return;

			MetaDataNode node = RegisterTile.Matrix.MetaDataLayer.Get(RegisterTile.LocalPositionClient);

			//Space or below -10 degrees celsius is safe from miasma creation
			if (node.IsSpace || node.GasMix.Temperature <= Reactions.KOffsetC - 10) return;

			//If we are in a container then don't produce miasma
			//TODO: make this only happen with coffins, body bags and other body containers (morgue, etc)
			if (objectBehaviour.ContainedInObjectContainer != null) return;

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
						$"<color=red>\n {theyPronoun} {part.gameObject.ExpensiveName()} is bleeding!</color>");
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

			var Race = playerScript.characterSettings.GetRaceSo();

			if (sickness.ImmuneRaces.Contains(Race)) return;

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


		private TemperatureAlert ExtremistTemperatureCash = TemperatureAlert.None;
		private PressureAlert ExtremistPressureCash = PressureAlert.None;

		public void ExposePressureTemperature(float EnvironmentalPressure, float EnvironmentalTemperature)
		{
			PressureAlert ExtremistPressure = PressureAlert.None;
			TemperatureAlert ExtremistTemperature = TemperatureAlert.None;
			var SurfaceBodyPartsCount = SurfaceBodyParts.Count;
			foreach (var bodyPart in SurfaceBodyParts)
			{
				var newTemperatureAlert = bodyPart.ExposeTemperature(EnvironmentalTemperature, SurfaceBodyPartsCount);
				var newPressureAlert = bodyPart.ExposePressure(EnvironmentalPressure, SurfaceBodyPartsCount);
				if (newPressureAlert != PressureAlert.None)
				{
					if (ExtremistPressure is not PressureAlert.PressureTooHigher or PressureAlert.PressureTooLow)
					{
						switch (ExtremistPressure)
						{
							case PressureAlert.PressureHigher or PressureAlert.PressureLow:
							{
								if (newPressureAlert is PressureAlert.PressureTooHigher or PressureAlert.PressureTooLow)
								{
									ExtremistPressure = newPressureAlert;
								}

								break;
							}
							case PressureAlert.None:
								ExtremistPressure = newPressureAlert;
								break;
						}
					}
				}


				if (newTemperatureAlert != TemperatureAlert.None)
				{
					if (ExtremistTemperature is not TemperatureAlert.TooHot or TemperatureAlert.TooCold)
					{
						switch (ExtremistTemperature)
						{
							case TemperatureAlert.Hot or TemperatureAlert.Cold:
							{
								if (newTemperatureAlert is TemperatureAlert.TooHot or TemperatureAlert.TooCold)
								{
									ExtremistTemperature = newTemperatureAlert;
								}

								break;
							}
							case TemperatureAlert.None:
								ExtremistTemperature = newTemperatureAlert;
								break;
						}
					}
				}
			}

			if (ExtremistPressure != ExtremistPressureCash)
			{
				var old = GetAlertSOFromPressure(ExtremistPressureCash);
				if (old != null)
				{
					BodyAlertManager.UnRegisterAlert(old);
				}

				ExtremistPressureCash = ExtremistPressure;

				var newOne = GetAlertSOFromPressure(ExtremistPressure);
				if (newOne != null)
				{
					BodyAlertManager.RegisterAlert(newOne);
				}
			}

			if (ExtremistTemperature != ExtremistTemperatureCash)
			{
				var old = GetAlertSOFromTemperature(ExtremistTemperatureCash);
				if (old != null)
				{
					BodyAlertManager.UnRegisterAlert(old);
				}

				ExtremistTemperatureCash = ExtremistTemperature;

				var newOne = GetAlertSOFromTemperature(ExtremistTemperature);
				if (newOne != null)
				{
					BodyAlertManager.RegisterAlert(newOne);
				}
			}
		}


		public AlertSO GetAlertSOFromTemperature(TemperatureAlert TemperatureAlert)
		{
			switch (TemperatureAlert)
			{
				case TemperatureAlert.Hot:
					return CommonAlertSOs.Instance.Temperature_Hot;
				case TemperatureAlert.TooHot:
					return CommonAlertSOs.Instance.Temperature_TooHot;
				case TemperatureAlert.Cold:
					return CommonAlertSOs.Instance.Temperature_Cold;
				case TemperatureAlert.TooCold:
					return CommonAlertSOs.Instance.Temperature_TooCold;
				default:
					return null;
			}
		}

		public AlertSO GetAlertSOFromPressure(PressureAlert PressureAlert)
		{
			switch (PressureAlert)
			{
				case PressureAlert.PressureTooHigher:
					return CommonAlertSOs.Instance.Pressure_TooHigher;
				case PressureAlert.PressureHigher:
					return CommonAlertSOs.Instance.Pressure_Higher;
				case PressureAlert.PressureLow:
					return CommonAlertSOs.Instance.Pressure_Low;
				case PressureAlert.PressureTooLow:
					return CommonAlertSOs.Instance.Pressure_TooLow;
				default:
					return null;
			}
		}


		public void IndicatePain(float dmgTaken, bool ignoreCooldown = false)
		{
			if (EmoteActionManager.Instance == null || screamEmote == null
			                                        || ConsciousState == ConsciousState.UNCONSCIOUS || IsDead) return;
			if (ignoreCooldown == false && canScream == false) return;
			if (dmgTaken < painScreamDamage) return;
			EmoteActionManager.DoEmote(screamEmote, playerScript.gameObject);
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
			playerScript.RegisterPlayer.ServerRemoveStun();
			if (OverallHealth > fastRegenThreshold) return;

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
			InitialSpecies = RaceBodyparts;
			foreach (var System in RaceBodyparts.Base.SystemSettings)
			{
				var newsys = System.CloneThisSystem();
				newsys.Base = this;
				newsys.InIt();
				ActiveSystems.Add(newsys);
			}

			meatProduce = RaceBodyparts.Base.MeatProduce;
			skinProduce = RaceBodyparts.Base.SkinProduce;
		}

		public void StartFresh()
		{
			foreach (var system in ActiveSystems)
			{
				system.StartFresh();
			}
		}

		//hummmm
		//How to handle Items being in item storage That being moved in and out of players item storage?
		//hummmmmmmmmmmmmmmmmmmmmmmmmmmmm
		//inherits ILeave body Enter body  // yes
		//Recursively sets stuff yeah good


		public void InstantiateAndSetUp(ObjectList ListToSpawn)
		{
			if (ListToSpawn != null && ListToSpawn.Elements.Count > 0)
			{
				foreach (var ToSpawn in ListToSpawn.Elements)
				{
					var bodyPartObject = Spawn.ServerPrefab(ToSpawn, spawnManualContents: true).GameObject;
					BodyPartStorage.ServerTryAdd(bodyPartObject);
				}
			}
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) ||
			    KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions,
				    KeyboardInputManager.KeyEventType.Hold) == false)
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

		private void ClownAbuseScoreEvent(DamageType damageType, GameObject abuser, float Amount)
		{
			if (abuser == null) return;
			if (damageType == DamageType.Clone || damageType == DamageType.Oxy ||
			    damageType == DamageType.Radiation) return;
			if (abuser.TryGetComponent<PlayerScript>(out var script) == false) return;
			if (script.gameObject == abuser) return; //Don't add to the score if the clown hits themselves.
			ScoreMachine.AddToScoreInt(Mathf.RoundToInt(-5 * Amount), RoundEndScoreBuilder.COMMON_SCORE_CLOWNABUSE);
		}

		public string HoverTip()
		{
			StringBuilder finalText = new StringBuilder();
			if (IsSoftCrit || IsCrit || IsDead)
			{
				var state = IsDead ? "They appear to be dead!" : "They appear to be in a critical condition!";
				finalText.AppendLine(state);
			}

			if (FireStacks > 0)
			{
				finalText.AppendLine("They are on fire!");
			}

			if (BleedStacks > 0)
			{
				finalText.AppendLine("They are bleeding!");
			}

			return finalText.ToString();
		}

		public string CustomTitle()
		{
			return IsDead == false ? null : $"{gameObject.ExpensiveName()} [dead]";
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			//TODO: add icon indicators for being lit on fire and being dead.
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			if (IsDead == false && IsCrit == false) return null;
			TextColor CPRText = new TextColor
			{
				Text = "Left-Click (Help Intent): Perform CPR.",
				Color = IntentColors.Help
			};
			List<TextColor> interactions = new List<TextColor>();
			interactions.Add(CPRText);
			return interactions;
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