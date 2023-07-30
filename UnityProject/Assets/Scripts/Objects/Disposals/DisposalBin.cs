using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using Systems.Disposals;
using AddressableReferences;
using Random = UnityEngine.Random;
using Messages.Server.SoundMessages;
using Systems.Atmospherics;
using Systems.Electricity;
using UI.Systems.Tooltips.HoverTooltips;

namespace Objects.Disposals
{
	public enum BinState
	{
		Disconnected,
		Off,
		Ready,
		Flushing,
		Recharging
	}

	public enum BinSprite
	{
		Upright = 0,
		Sideways = 1,
		Flushing = 2
	}

	public enum BinOverlaySprite
	{
		Ready = 0,
		Charging = 1,
		Full = 2,
		Handle = 3
	}

	public class DisposalBin : DisposalMachine, IExaminable, ICheckedInteractable<MouseDrop>, IEscapable, IBumpableObject,
		IAPCPowerable, IHoverTooltip
	{
		private const int CHARGED_PRESSURE = 200; // kPa
		private const int AUTO_FLUSH_DELAY = 2;
		private const int MAX_RECHARGE_TIME = 8;
		private const float ANIMATION_TIME = 1.3f; // As per sprite sheet JSON file.

		[SerializeField]
		private AddressableAudioSource rechargeSFX = null;
		[SerializeField]
		private AddressableAudioSource AirFlapSound = null;
		[SerializeField]
		private AddressableAudioSource FlushSound = null;
		[SerializeField]
		[Tooltip("The sound when throwing things in the bin.")]
		private List<AddressableAudioSource> trashDunkSounds = null;
		[SerializeField]
		[Tooltip("The sound when the item doesn't fall into the trash can.")]
		private AddressableAudioSource trashDunkMissSound = null;

		[field: SerializeField] public APCPoweredDevice PoweredDevice { get; private set; }
		[SerializeField] private float wattageUseage = 1500;

		private string runLoopGUID = "";

		private HasNetworkTab netTab;
		private SpriteHandler overlaysSpriteHandler;

		private Coroutine autoFlushCoroutine;
		private Coroutine rechargeCoroutine;

		// GUI instances can listen to this, to update UI state.
		public event Action BinStateUpdated;

		// We sync binState so that the client knows when to play or stop the recharging SFX.
		[SyncVar(hook = nameof(OnSyncBinState))]
		private BinState binState = BinState.Disconnected;
		[SyncVar]
		private float chargePressure = 0;

		public BinState BinState => binState;

		public bool PowerDisconnected => binState is BinState.Disconnected;

		public bool PowerOff => binState == BinState.Off || turnedOff;
		public bool BinReady => binState == BinState.Ready;
		public bool BinFlushing => binState == BinState.Flushing;
		public bool BinCharging => binState == BinState.Recharging;

		/// <summary>
		/// If the bin is already connected to power, it is only screwdriverable if it is set to off.
		/// This allows the screwdriver to be disposed of during normal operations.
		/// </summary>
		public bool Screwdriverable => MachineSecured && (PowerDisconnected || PowerOff);
		public float ChargePressure => chargePressure;
		public bool BinCharged => chargePressure >= CHARGED_PRESSURE;

		private float RandomDunkPitch => Random.Range(0.7f, 1.2f);

		/// <summary>
		/// Checks if the bin is turned off locally. Does not relate to APC functionality.
		/// </summary>
		private bool turnedOff = true;

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();
			netTab = GetComponent<HasNetworkTab>();
			overlaysSpriteHandler = transform.GetChild(1).GetComponent<SpriteHandler>();
			PoweredDevice = GetComponent<APCPoweredDevice>();
			if (PoweredDevice.RelatedAPC == null)
			{
				SetBinState(BinState.Disconnected);
			}
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			// Assume bin starts unanchored and therefore UI is inaccessable.
			netTab.enabled = false;
			UpdateSpriteBinState();

			base.OnSpawnServer(info);
		}

		protected override void SpawnMachineAsInstalled()
		{
			base.SpawnMachineAsInstalled();

			chargePressure = CHARGED_PRESSURE;
			SetBinState(BinState.Ready);
		}

		#endregion Lifecycle

		#region Sync

		private void OnSyncBinState(BinState oldState, BinState newState)
		{
			binState = newState;

			if (BinCharging)
			{
				runLoopGUID = Guid.NewGuid().ToString();
				SoundManager.PlayAtPositionAttached(rechargeSFX, registerObject.WorldPosition, gameObject, runLoopGUID);
			}
			else
			{
				SoundManager.Stop(runLoopGUID);
			}
		}

		#endregion Sync

		private void SetBinState(BinState newState)
		{
			binState = newState;
			UpdateSpriteBinState();
		}

		#region Sprites

		protected override void UpdateSpriteConstructionState()
		{
			UpdateSpriteBinState();
		}

		private void UpdateSpriteBinState()
		{
			if (MachineUnattached)
			{
				baseSpriteHandler.ChangeSprite((int) BinSprite.Sideways);
				overlaysSpriteHandler.PushClear();
				return;
			}

			var baseSprite = binState == BinState.Flushing ? BinSprite.Flushing : BinSprite.Upright;
			var overlaySprite = binState switch
			{
				BinState.Ready => BinOverlaySprite.Ready,
				BinState.Recharging => BinOverlaySprite.Charging,
				_ => BinOverlaySprite.Ready,
			};

			baseSpriteHandler.ChangeSprite((int)baseSprite);
			overlaysSpriteHandler.ChangeSprite((int)overlaySprite);
			overlaysSpriteHandler.PushTexture();

			BinStateUpdated?.Invoke();
		}

		#endregion Sprites

		#region Interactions

		// Click on disposal bin
		public override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject == null) return false;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;


			if (base.WillInteract(interaction, side)) return true;
			// Bin accepts all items for disposal.
			return MachineSecured;
		}

		// Click on disposal bin
		public override void ServerPerformInteraction(PositionalHandApply interaction)
		{
			currentInteraction = interaction;

			if (interaction.HandObject != null && interaction.HandObject.TryGetComponent<InteractableStorage>(out var storage) && interaction.Intent != Intent.Harm)
			{
				storage.ItemStorage.ServerDropAllAtWorld(gameObject.AssumedWorldPosServer());
				objectContainer.GatherObjects();
				Chat.AddExamineMsg(interaction.Performer, "You throw all of the bag's contents into the disposal bin.");
				return;
			}

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) && MachineWrenchable)
			{
				TryUseWrench();
			}
			else if (Validations.HasUsedActiveWelder(interaction) && MachineWeldable)
			{
				TryUseWelder();
			}
			else if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) && Screwdriverable)
			{
				TryUseScrewdriver();
			}
			else if (MachineSecured &&  interaction.Intent != Intent.Harm)
			{
				Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
				StoreItem(interaction.UsedObject);
			}
		}

		// Drag something and drop on disposal bin
		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (interaction.TargetObject == null) return false;

			if (Validations.IsReachableByRegisterTiles(
					interaction.Performer.RegisterTile(),
					interaction.UsedObject.RegisterTile(),
					side == NetworkSide.Server) == false) return false;

			if (Validations.IsReachableByRegisterTiles(
				interaction.Performer.RegisterTile(),
				interaction.TargetObject.RegisterTile(),
				side == NetworkSide.Server) == false) return false;

			return true;
		}

		// Drag something and drop on disposal bin
		public void ServerPerformInteraction(MouseDrop interaction)
		{
			if (interaction.UsedObject == null) return;
			if (interaction.UsedObject.TryGetComponent<PlayerScript>(out _) == false) return; // Test to see if player

			// Don't store player unless secured.
			if (MachineSecured == false) return;
			StartStoringPlayer(interaction);
		}

		public override string Examine(Vector3 worldPos = default)
		{
			string baseString = "It";
			if (FloorPlatingExposed())
			{
				baseString = base.Examine().TrimEnd('.') + " and";
			}

			switch (BinState)
			{
				case BinState.Disconnected:
					return $"{baseString} is disconnected from the power.";
				case BinState.Off:
					return $"{baseString} the power is switched off.";
				case BinState.Ready:
					return $"{baseString} is ready for use.";
				case BinState.Flushing:
					return $"{baseString} is currently flushing its contents.";
				case BinState.Recharging:
					return $"{baseString} is currently restoring pressure.";
			}

			return baseString;
		}

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			if (BinFlushing)
			{
				Chat.AddExamineMsgFromServer(
						entity,
						"You're too late! A blast of oily air hits you with force... You start to lose your grip...");
				return;
			}

			objectContainer.RetrieveObject(entity);
		}

		#endregion Interactions

		// gives the probability of an object falling into the bin. Yes, it's like basketball
		public void OnBump(GameObject item, GameObject client)
		{
			if (isServer == false) return;
			if (MachineSecured == false) return;

			if (item.GetComponent<UniversalObjectPhysics>().IsFlyingSliding)
			{
				if (DMMath.Prob(25))
				{
					Chat.AddActionMsgToChat(gameObject, $"The {item.ExpensiveName()} bounces off the rim of the {gameObject.ExpensiveName()}!");
					var dunkMissParameters = new AudioSourceParameters(pitch: RandomDunkPitch);
					SoundManager.PlayNetworkedAtPos(trashDunkMissSound, registerObject.WorldPositionServer, dunkMissParameters);
					return;
				}

				Chat.AddActionMsgToChat(gameObject, $"The {item.ExpensiveName()} goes straight into the {gameObject.ExpensiveName()}! Score!");
				StoreItem(item);
			}
		}

		private void StoreItem(GameObject item)
		{
			objectContainer.StoreObject(item);

			AudioSourceParameters dunkParameters = new AudioSourceParameters(pitch: RandomDunkPitch);
			SoundManager.PlayNetworkedAtPos(trashDunkSounds, gameObject.AssumedWorldPosServer(), dunkParameters);

			this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
		}

		// TODO This was copied from somewhere. Where?
		private void StartStoringPlayer(MouseDrop interaction)
		{
			Vector3Int targetObjectLocalPosition = interaction.TargetObject.RegisterTile().LocalPosition;
			Vector3Int targetObjectWorldPos = interaction.TargetObject.AssumedWorldPosServer().CutToInt();

			// We check if there's nothing in the way, like another player or a directional window.
			if (interaction.UsedObject.RegisterTile().Matrix.IsPassableAtOneMatrixOneTile(targetObjectLocalPosition, true, context: gameObject) == false)
			{
				return;
			}

			// Runs when the progress action is complete.
			void StoringPlayer()
			{
				if (interaction.UsedObject.TryGetComponent<PlayerScript>(out var playerScript))
				{
					if (playerScript.RegisterPlayer.Matrix.IsPassableAtOneMatrixOneTile(targetObjectLocalPosition, true, context: gameObject))
					{
						playerScript.PlayerSync.AppearAtWorldPositionServer(targetObjectWorldPos, false);
					}
				}
				else
				{
					var transformComp = interaction.UsedObject.GetComponent<UniversalObjectPhysics>();
					if (transformComp != null)
					{
						transformComp.AppearAtWorldPositionServer(targetObjectWorldPos);
					}
				}

				StorePlayer(interaction);
			}

			StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, StoringPlayer).ServerStartProgress(interaction.UsedObject.RegisterTile(), 2, interaction.Performer);
		}

		private void StorePlayer(MouseDrop interaction)
		{
			objectContainer.StoreObject(interaction.DroppedObject);

			this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
		}

		#region UI

		public void FlushContents()
		{
			if (BinReady)
			{
				StartCoroutine(RunFlushSequence());
			}
		}

		public void EjectContents()
		{
			if (autoFlushCoroutine != null)
			{
				StopCoroutine(autoFlushCoroutine);
			}
			if (BinFlushing) return;

			objectContainer.RetrieveObjects();
		}

		public void TogglePower()
		{
			ToggleTurnedOffState();
			if (PowerOff)
			{
				TurnPowerOn();
			}
			else
			{
				TurnPowerOff();
			}
		}

		private void ToggleTurnedOffState()
		{
			turnedOff = !turnedOff;
			if (turnedOff)
			{
				overlaysSpriteHandler.PushClear();
			}
			else
			{
				overlaysSpriteHandler.ChangeSprite(0);
				overlaysSpriteHandler.PushTexture();
			}
		}

		#endregion UI

		private void TurnPowerOn()
		{
			// Cannot turn the pump on if power is not connected.
			if (PowerDisconnected) return;

			if (BinCharged)
			{
				SetBinState(BinState.Ready);
				if (objectContainer.IsEmpty == false)
				{
					this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
				}
			}
			else
			{
				SetBinState(BinState.Recharging);
				this.RestartCoroutine(Recharge(), ref rechargeCoroutine);
			}
		}

		private void TurnPowerOff()
		{
			// Cannot disable power while flushing
			if (BinFlushing) return;

			if (autoFlushCoroutine != null) StopCoroutine(autoFlushCoroutine);
			if (rechargeCoroutine != null) StopCoroutine(rechargeCoroutine);
			PoweredDevice.UpdateSynchronisedState(PoweredDevice.State, PowerState.Off);
			SetBinState(BinState.Off);
		}

		private IEnumerator Recharge()
		{
			var rechargeTime = 0;
			PoweredDevice.Wattusage += wattageUseage;
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} starts humming as it sucks the surrounding air into it.");
			while (BinCharging && BinCharged == false || rechargeTime < MAX_RECHARGE_TIME)
			{
				yield return WaitFor.Seconds(1);
				rechargeTime += 1;
				OperateAirPump();
			}

			if (PowerOff == false || PowerDisconnected == false)
			{
				SetBinState(BinState.Ready);
				this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
			}

			// Sound of the bin's air intake flap closing.
			SoundManager.PlayNetworkedAtPos(AirFlapSound, registerObject.WorldPositionServer, sourceObj: gameObject);
			Chat.AddActionMsgToChat(gameObject, $"The {gameObject.ExpensiveName()} closes its air intake's flaps.");
			PoweredDevice.Wattusage -= wattageUseage;
		}

		private void OperateAirPump()
		{
			MetaDataLayer metadata = registerObject.Matrix.MetaDataLayer;
			GasMix tileMix = metadata.Get(registerObject.LocalPositionServer, false).GasMix;

			var molesToTransfer = (tileMix.Moles - (tileMix.Moles * (CHARGED_PRESSURE / gasContainer.GasMix.Pressure))) * -1;
			molesToTransfer *= 0.5f;

			GasMix.TransferGas(gasContainer.GasMix, tileMix, molesToTransfer.Clamp(0, 8));
			metadata.UpdateSystemsAt(registerObject.LocalPositionServer, SystemType.AtmosSystem);

			chargePressure = gasContainer.GasMix.Pressure;
		}

		private IEnumerator RunFlushSequence()
		{
			// Bin orifice closes...
			SetBinState(BinState.Flushing);
			yield return WaitFor.Seconds(ANIMATION_TIME);

			// Bin orifice closed. Release the charge.
			chargePressure = 0;
			SoundManager.PlayNetworkedAtPos(FlushSound, registerObject.WorldPositionServer, sourceObj: gameObject);
			DisposalsManager.Instance.NewDisposal(gameObject);

			// Restore charge.
			SetBinState(BinState.Recharging);
			StartCoroutine(Recharge());
		}

		private IEnumerator AutoFlush()
		{
			yield return WaitFor.Seconds(AUTO_FLUSH_DELAY);
			if (binState is BinState.Off or BinState.Disconnected) yield break;
			if (BinReady && objectContainer.IsEmpty == false)
			{
				StartCoroutine(RunFlushSequence());
			}
		}

		#region Construction

		private void TryUseScrewdriver()
		{
			// Assume binState is Secured
			string finishPerformerMsg = $"You connect the {objectAttributes.InitialName} to the power.";
			string finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} connects the " +
						$"{objectAttributes.InitialName} to the power.";

			if (PowerDisconnected == false)
			{
				finishPerformerMsg = $"You disconnect the {objectAttributes.InitialName} from the power.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} disconnects the " +
						$"{objectAttributes.InitialName} from the power.";
			}

			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, 0, "", "", finishPerformerMsg, finishOthersMsg, () => UseScrewdriver());
		}

		private void UseScrewdriver()
		{
			// Advance construction state by connecting power.
			if (PowerDisconnected)
			{
				SetBinState(BinState.Off);
			}

			// Retard construction state - deconstruction beginning - by disconnecting power.
			else
			{
				SetBinState(BinState.Disconnected);
			}
		}

		protected override void SetMachineInstalled()
		{
			base.SetMachineInstalled();
			SetBinState(BinState.Disconnected);
			netTab.enabled = true;
		}

		protected override void SetMachineUninstalled()
		{
			base.SetMachineUninstalled();
			SetBinState(BinState.Disconnected);
			netTab.enabled = false;
		}

		#endregion Construction

		public void PowerNetworkUpdate(float voltage)
		{
			//Does not require any updates related to voltage currently. State works just fine.
		}

		public void StateUpdate(PowerState state)
		{
			//This is to avoid StateUpdate turning bins back on even though a player told it to stay off.
			if (PowerOff) return;
			SetBinState(state == PowerState.Off ? BinState.Off : BinState.Ready);
		}

		public void OnAPCLinked()
		{
			turnedOff = false;
			_ = APCInit();
		}

		private async Task APCInit()
		{
			//Power updates take at least two seconds to update properly; especially when the round starts.
			//This isn't an issue when connecting APCs using multi-tools or during normal gameplay. But for other cases at the start of the round, this delay is needed.
			if (GameManager.Instance.RoundTimeInMinutes < 2) await Task.Delay(2500);
			StateUpdate(PoweredDevice.State);
		}

		public void OnAPCUnlinked()
		{
			SetBinState(BinState.Disconnected);
		}

		public string HoverTip()
		{
			var onOffText = PowerOff ? "off" : "on";
			return $"It appears to be powered {onOffText}";
		}

		public string CustomTitle() => null;
		public Sprite CustomIcon() => null;
		public List<Sprite> IconIndicators() => null;

		public List<TextColor> InteractionsStrings()
		{
			List<TextColor> interactions = new List<TextColor>()
			{
				new TextColor()
				{
					Text = "Click on bin with object in hand to dispose of it.",
					Color = Color.green
				},

				new TextColor()
				{
					Text = "Click on bin with empty hand to view controls.",
					Color = Color.green
				},
				new TextColor()
				{
					Text = "Throw an object at the bin to dispose of it from afar.",
					Color = Color.blue
				},
			};
			return interactions;
		}
	}
}
