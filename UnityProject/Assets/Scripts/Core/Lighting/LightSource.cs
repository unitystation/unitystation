using System;
using AddressableReferences;
using Core;
using Core.Lighting;
using UnityEngine;
using Random = UnityEngine.Random;
using Mirror;
using ScriptableObjects;
using Light2D;
using Logs;
using Messages.Server.SoundMessages;
using Systems.Electricity;
using Shared.Systems.ObjectConnection;
using Objects.Construction;
using UnityEngine.Serialization;


namespace Objects.Lighting
{
	/// <summary>
	/// Component responsible for the behaviour of light tubes / bulbs in particular.
	/// </summary>
	public class LightSource : ObjectTrigger, ICheckedInteractable<HandApply>, IAPCPowerable, IServerLifecycle,
		IMultitoolSlaveable
	{
		[SyncVar(hook = nameof(SetColor)), SerializeField, FormerlySerializedAs("ONColour")]
		public Color CurrentOnColor;

		[SyncVar(hook = nameof(SyncEmergencyColour))]
		public Color EmergencyColour;

		public LightSwitchV2 relatedLightSwitch;

		[SerializeField] private LightMountState InitialState = LightMountState.On;

		[field: SyncVar(hook = nameof(SyncLightState))]
		public LightMountState MountState { get; private set; }

		[Header("Generates itself if this is null:")]
		public GameObject mLightRendererObject;

		[SerializeField] private bool isWithoutSwitch = true;
		public bool IsWithoutSwitch => isWithoutSwitch;
		private bool switchState = true;
		private PowerState powerState;
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		private EmergencyLightAnimator EmergencyLightAnimator;
		[field: SerializeField] public LightAnimator Animator { get; private set; }
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private SpriteRenderer spriteRendererLightOn;
		[SerializeField] private Integrity integrity = default;
		public Integrity Integrity => integrity;
		[SerializeField] private Rotatable directional;
		[SerializeField] private BoxCollider2D boxColl = null;
		[SerializeField] private Vector4 collDownSetting = Vector4.zero;
		[SerializeField] private Vector4 collRightSetting = Vector4.zero;
		[SerializeField] private Vector4 collUpSetting = Vector4.zero;
		[SerializeField] private Vector4 collLeftSetting = Vector4.zero;
		[SerializeField] private SpritesDirectional spritesStateOnEffect = null;
		[SerializeField] private SOLightMountStatesMachine mountStatesMachine = null;
		[SerializeField, Range(0, 100f)] private float maximumDamageOnTouch = 3f;

		[SerializeField] private GameObject sparkObject = null;

		private SOLightMountState currentState;
		public SOLightMountState CurrentState => currentState;
		private UniversalObjectPhysics objectPhysics;
		private LightFixtureConstruction construction;

		private ItemTrait traitRequired;
		public ItemTrait TraitRequired => traitRequired;
		private GameObject itemInMount;
		public LightSprite LightSpriteUsed { get; private set; }

		public float integrityThreshBar { get; private set; }

		private bool sparking = false;

		[Header("Audio")] [SerializeField] private AddressableAudioSource ambientSoundWhileOn;
		[SerializeField] private AddressableAudioSource turnOffOnNoise;
		private string loopKey;

		private bool SoundInit = false;

		#region Lifecycle

		private void Awake()
		{
			EmergencyLightAnimator = this.GetComponent<EmergencyLightAnimator>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			construction = GetComponent<LightFixtureConstruction>();
			if (mLightRendererObject == null)
			{
				mLightRendererObject = LightSpriteBuilder.BuildDefault(gameObject, new Color(0, 0, 0, 0), 12);
			}

			LightSpriteUsed = mLightRendererObject.GetComponent<LightSprite>();
			if (isWithoutSwitch == false)
			{
				switchState = InitialState == LightMountState.On;
			}

			ChangeCurrentState(InitialState);
			traitRequired = currentState.TraitRequired;
			RefreshBoxCollider();
			loopKey = Guid.NewGuid().ToString();
			ComponentsTracker<LightSource>.Instances.Add(this);
		}

		private void Start()
		{
			LightSpriteUsed.Color = CurrentOnColor;
			CheckAudioState();
		}

		private void OnEnable()
		{
			directional.OnRotationChange.AddListener(OnDirectionChange);
			integrity.OnApplyDamage.AddListener(OnDamageReceived);
		}

		private void OnDisable()
		{
			directional.OnRotationChange.RemoveListener(OnDirectionChange);
			if (integrity != null) integrity.OnApplyDamage.RemoveListener(OnDamageReceived);

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TrySpark);
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncLightState(MountState, MountState);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (info.SpawnItems == false)
			{
				MountState = LightMountState.MissingBulb;
			}
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			Spawn.ServerPrefab(currentState.LootDrop, gameObject.RegisterTile().WorldPositionServer);
			UnSubscribeFromSwitchEvent();
			SoundManager.StopNetworked(loopKey);
		}

		private void OnDestroy()
		{
			ComponentsTracker<LightSource>.Instances.Remove(this);
		}

		#endregion

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.LightSwitch;
		IMultitoolMasterable IMultitoolSlaveable.Master => relatedLightSwitch;
		bool IMultitoolSlaveable.RequireLink => false;

		bool IMultitoolSlaveable.TrySetMaster(GameObject performer, IMultitoolMasterable master)
		{
			SetMaster(master);
			return true;
		}

		void IMultitoolSlaveable.SetMasterEditor(IMultitoolMasterable master)
		{
			SetMaster(master);
		}

		private void SetMaster(IMultitoolMasterable master)
		{
			if (master is LightSwitchV2 lightSwitch && lightSwitch != relatedLightSwitch)
			{
				SubscribeToSwitchEvent(lightSwitch);
			}
			else if (relatedLightSwitch != null)
			{
				UnSubscribeFromSwitchEvent();
			}
		}

		#endregion

		private void OnDirectionChange(OrientationEnum newDir)
		{
			SetSprites();
		}

		[Server]
		public void ServerChangeLightState(LightMountState newState)
		{
			SyncLightState(MountState, newState);

			if (newState == LightMountState.Broken)
			{
				UpdateManager.Add(TrySpark, 1f);
				sparking = true;
			}
			else
			{
				if (sparking)
				{
					UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, TrySpark);
				}

				sparking = false;
			}
		}

		public bool HasBulb()
		{
			return MountState != LightMountState.MissingBulb && MountState != LightMountState.None;
		}

		private void SyncLightState(LightMountState oldState, LightMountState newState)
		{
			MountState = newState;
			ChangeCurrentState(newState);
			SetSprites();
			SetColor(CurrentOnColor, CurrentOnColor);
			mLightRendererObject.SetActive(newState is LightMountState.On or LightMountState.BurnedOut);
			mLightRendererObject.gameObject.SetActive(newState is LightMountState.On or LightMountState.BurnedOut);
			if (newState == LightMountState.BurnedOut)
			{
				Animator.ServerPlayAnim(1);
			}
			else if (Animator.ActiveAnimation is {ID: 1})
			{
				Animator.ServerStopAnim();
			}
			CheckAudioState();
			if (newState == LightMountState.On)
			{
				SoundManager.PlayNetworkedAtPos(turnOffOnNoise, gameObject.AssumedWorldPosServer(), new AudioSourceParameters().PitchVariation(0.05f));
			}
		}

		private void ChangeCurrentState(LightMountState newState)
		{
			if (mountStatesMachine.LightMountStates.Contains(newState))
			{
				currentState = mountStatesMachine.LightMountStates[newState];
			}
		}

		public void RefreshBoxCollider()
		{
			directional = GetComponent<Rotatable>();
			Vector2 offset = Vector2.zero;
			Vector2 size = Vector2.zero;


			offset = new Vector2(collUpSetting.x, collUpSetting.y);
			size = new Vector2(collUpSetting.z, collUpSetting.w);

			boxColl.offset = offset;
			boxColl.size = size;
		}

		public void SetSprites()
		{
			spriteHandler.SetSpriteSO(currentState.SpriteData, null);
			spriteRendererLightOn.sprite = MountState == LightMountState.On
				? spritesStateOnEffect.sprites[0]
				: null;

			itemInMount = currentState.Tube;

			var currentMultiplier = currentState.MultiplierIntegrity;
			if (currentMultiplier > 0.15f)
			{
				integrityThreshBar = integrity.initialIntegrity * currentMultiplier;
			}

			RefreshBoxCollider();
		}


		public void SyncEmergencyColour(Color oldState, Color newState)
		{
			EmergencyColour = newState;
		}

		public void SetColor(Color oldState, Color newState)
		{
			CurrentOnColor = newState;
			LightSpriteUsed.Color = newState;
		}

		private void CheckAudioState()
		{
			if (isServer)
			{
				if (MountState == LightMountState.On)
				{
					if (SoundInit)
					{
						SoundManager.TokenPlayNetworked(loopKey);
					}
					else
					{
						SoundManager.ClientPlayAtPositionAttached(ambientSoundWhileOn,
							gameObject.RegisterTile().WorldPosition, gameObject, loopKey, false, false);
						SoundInit = true;
					}

				}
				else
				{
					SoundManager.StopNetworked(loopKey, false);
				}
			}
		}

		#region ICheckedInteractable<HandApply>

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (!construction.IsFullyBuilt()) return false;
			if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false;
			if (interaction.HandObject != null &&
			    !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.LightReplacer) &&
			    !Validations.HasItemTrait(interaction.HandObject, traitRequired)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null && MountState is not LightMountState.MissingBulb or LightMountState.None)
			{
				TryRemoveBulb(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.LightReplacer))
			{
				TryReplaceBulb(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, traitRequired))
			{
				TryAddBulb(interaction);
			}
		}

		private void TryRemoveBulb(HandApply interaction)
		{
			if (MountState is LightMountState.None or LightMountState.MissingBulb) return;
			try
			{
				//(Gilles)  : the hand that we use to interact and hold items isn't the same entity as the slot where you wear gloves.
				//(MaxIsJoe): GetActiveHand() retrieves the slot you hold and use items with not the slot that you use to wear gloves.
				//TODO : According to Gilles this is conceptually wrong and should be dealt with sometime in the future.
				var handSlots = interaction.PerformerPlayerScript.DynamicItemStorage.GetNamedItemSlots(NamedSlot.hands);

				bool HasGlove()
				{
					foreach (var slot in handSlots)
					{
						if (interaction.PerformerPlayerScript.playerHealth.brain != null &&
						    interaction.PerformerPlayerScript.playerHealth.brain.HasTelekinesis)
						{
							Chat.AddExamineMsg(interaction.Performer,
								"You instinctively use your telekinetic power to protect your hand from getting burnt.");
							return true;
						}

						if (slot.IsEmpty) continue;
						if (Validations.HasItemTrait(slot.ItemObject, CommonTraits.Instance.BlackGloves)) return true;
					}

					return false;
				}

				if (MountState is LightMountState.On && HasGlove() == false)
				{
					float damage = Random.Range(0, maximumDamageOnTouch);
					var playerHealth = interaction.PerformerPlayerScript.playerHealth;
					var burntBodyPart = interaction.HandSlot.NamedSlot == NamedSlot.leftHand
						? BodyPartType.LeftArm
						: BodyPartType.RightArm;
					playerHealth.ApplyDamageToBodyPart(gameObject, damage, AttackType.Energy, DamageType.Burn,
						burntBodyPart);

					Chat.AddExamineMsgFromServer(interaction.Performer,
						"<color=red>You burn your hand on the bulb while attempting to remove it!</color>");
					return;
				}
				var spawnedItem = Spawn.ServerPrefab(itemInMount, interaction.Performer.AssumedWorldPosServer())
					.GameObject;

				var lightTubeData = spawnedItem.GetComponent<LightTubeData>();
				if (lightTubeData != null)
				{
					lightTubeData.RegularColour = CurrentOnColor;
					lightTubeData.EmergencyColour = EmergencyColour;
				}

				ItemSlot bestHand = interaction.PerformerPlayerScript.DynamicItemStorage.GetBestHand();
				if (bestHand != null && spawnedItem != null)
				{
					Inventory.ServerAdd(spawnedItem, bestHand);
				}

				ServerChangeLightState(LightMountState.MissingBulb);
			}
			catch (NullReferenceException exception)
			{
				Loggy.LogError(
					$"A NRE was caught in LightSource.TryRemoveBulb(): {exception.Message} \n {exception.StackTrace}",
					Category.Lighting);
			}
		}

		private void TryAddBulb(HandApply interaction)
		{
			if (MountState != LightMountState.MissingBulb) return;

			var lightTubeData = interaction.HandObject.GetComponent<LightTubeData>();
			if (lightTubeData != null)
			{
				SetColor(CurrentOnColor, lightTubeData.RegularColour);
				SyncEmergencyColour(EmergencyColour, lightTubeData.EmergencyColour);
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Broken))
			{
				ServerChangeLightState(LightMountState.Broken);
			}
			else
			{
				ServerChangeLightState(
					(switchState && (powerState == PowerState.On))
						? LightMountState.On
						: LightMountState.Off);
			}

			_ = Despawn.ServerSingle(interaction.HandObject);
		}

		public void TryAddBulb(GameObject lightBulb) //NOTE Only used by Advanced light Replacer so Colour is inherited from  Advanced light Replacer
		{
			if (MountState != LightMountState.MissingBulb) return;

			if (Validations.HasItemTrait(lightBulb, CommonTraits.Instance.Broken))
			{
				ServerChangeLightState(LightMountState.Broken);
			}
			else
			{
				ServerChangeLightState(
					(switchState && (powerState == PowerState.On))
						? LightMountState.On
						: LightMountState.Off);
			}

			_ = Despawn.ServerSingle(lightBulb);
		}

		public bool TryReplaceBulb(HandApply interaction)
		{
			if (MountState == LightMountState.MissingBulb) return false;
			Spawn.ServerPrefab(itemInMount, interaction.Performer.AssumedWorldPosServer());
			ServerChangeLightState(LightMountState.MissingBulb);
			return true;
		}

		#endregion

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage)
		{

		}

		public void StateUpdate(PowerState newPowerState)
		{
			if (isServer == false) return;
			powerState = newPowerState;
			if (MountState == LightMountState.Broken
			    || MountState == LightMountState.MissingBulb) return;
			switch (newPowerState)
			{
				case PowerState.Off:
					Animator.ServerStopAnim();
					ServerChangeLightState(LightMountState.Off);
					return;
				case PowerState.LowVoltage:
					Animator.ServerPlayAnim(0);
					ServerChangeLightState(LightMountState.Off);
					return;
				case PowerState.On:
					ServerChangeLightState(LightMountState.On);
					Animator.ServerStopAnim();
					return;
				case PowerState.OverVoltage:
					ServerChangeLightState(LightMountState.BurnedOut);
					Animator.ServerStopAnim();
					return;
			}
		}

		#endregion

		#region SwitchRelatedLogic

		public void SubscribeToSwitchEvent(LightSwitchV2 lightSwitch)
		{
			UnSubscribeFromSwitchEvent();
			relatedLightSwitch = lightSwitch;
			lightSwitch.SwitchTriggerEvent += Trigger;
		}

		public void UnSubscribeFromSwitchEvent()
		{
			if (relatedLightSwitch == null) return;
			relatedLightSwitch.SwitchTriggerEvent -= Trigger;
			relatedLightSwitch = null;
		}

		public override void Trigger(bool newState)
		{
			if (isServer == false) return;
			switchState = newState;
			if (MountState == LightMountState.On || MountState == LightMountState.Off)
				ServerChangeLightState(newState ? LightMountState.On : LightMountState.Off);
		}

		#endregion

		#region Spark

		private void TrySpark()
		{
			//Has to be broken and have power to spark
			if (MountState != LightMountState.Broken || powerState == PowerState.Off) return;

			InternalSpark(30f);
		}

		private void InternalSpark(float chanceToSpark)
		{
			//Clamp just in case
			chanceToSpark = Mathf.Clamp(chanceToSpark, 1, 100);

			//E.g will have 25% chance to not spark when chanceToSpark = 75
			if (DMMath.Prob(100 - chanceToSpark)) return;

			//Try start fire if possible
			var reactionManager = objectPhysics.registerTile.Matrix.ReactionManager;
			reactionManager.ExposeHotspot(objectPhysics.registerTile.LocalPositionServer, 1000);

			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Sparks,
				objectPhysics.registerTile.WorldPositionServer,
				sourceObj: gameObject);

			if (CustomNetworkManager.IsHeadless == false)
			{
				sparkObject.SetActive(true);
			}

			ClientRpcSpark();
		}

		[ClientRpc]
		private void ClientRpcSpark()
		{
			sparkObject.SetActive(true);
		}

		#endregion

		private void OnDamageReceived(DamageInfo arg0)
		{
			if (CustomNetworkManager.IsServer == false) return;

			CheckIntegrityState(arg0);
		}

		private void CheckIntegrityState(DamageInfo arg0)
		{
			if (integrity.integrity > integrityThreshBar || MountState == LightMountState.MissingBulb) return;
			Vector3 pos = gameObject.AssumedWorldPosServer();

			if (MountState == LightMountState.Broken)
			{
				ServerChangeLightState(LightMountState.MissingBulb);
				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.GlassStep, pos, sourceObj: gameObject);
			}
			else
			{
				ServerChangeLightState(LightMountState.Broken);
				Spawn.ServerPrefab(CommonPrefabs.Instance.GlassShard, pos, count: Random.Range(0, 2),
					scatterRadius: Random.Range(0, 2));
				//Because this can get destroyed by fire then it tries accessing the tile safe loop and Complaints
				if (arg0.AttackType != AttackType.Fire)
				{
					TrySpark();
				}
			}
		}
	}
}