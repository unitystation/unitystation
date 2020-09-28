using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace Disposals
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

	[ExecuteInEditMode]
	public class DisposalBin : DisposalMachine, IServerDespawn, IExaminable, ICheckedInteractable<MouseDrop>
	{
		const int CHARGED_PRESSURE = 600; // kPa
		const int AUTO_FLUSH_DELAY = 2;
		const float ANIMATION_TIME = 1.3f; // As per sprite sheet JSON file.

		[SerializeField] AudioSource rechargeSFX = null;

		HasNetworkTab netTab;
		SpriteHandler overlaysSpriteHandler;

		Coroutine autoFlushCoroutine;
		Coroutine rechargeCoroutine;

		// GUI instances can listen to this, to update UI state.
		public event Action BinStateUpdated;

		// We sync binState so that the client knows when to play or stop the recharging SFX.
		[SyncVar(hook = nameof(OnSyncBinState))]
		BinState binState = BinState.Disconnected;
		[SyncVar]
		int chargePressure = 0;

		DisposalVirtualContainer virtualContainer;

		public BinState BinState => binState;
		public bool PowerDisconnected => binState == BinState.Disconnected;
		public bool PowerOff => binState == BinState.Off;
		public bool BinReady => binState == BinState.Ready;
		public bool BinFlushing => binState == BinState.Flushing;
		public bool BinCharging => binState == BinState.Recharging;
		public override bool MachineWeldable => base.MachineWeldable && PowerDisconnected;
		/// <summary>
		/// If the bin is already connected to power, it is only screwdriverable if it is set to off.
		/// This allows the screwdriver to be disposed of during normal operations.
		/// </summary>
		public bool Screwdriverable => MachineSecured && (PowerDisconnected || PowerOff);
		public int ChargePressure => chargePressure;
		public bool BinCharged => chargePressure >= CHARGED_PRESSURE;
		public bool ServerHasContents => virtualContainer != null ? virtualContainer.HasContents : false;

		#region Lifecycle

		protected override void Awake()
		{
			base.Awake();
			netTab = GetComponent<HasNetworkTab>();
			overlaysSpriteHandler = transform.GetChild(1).GetComponent<SpriteHandler>();
		}

		public override void OnStartServer()
		{
			// Assume bin starts unanchored and therefore UI is inaccessable.
			netTab.enabled = false;
			base.OnStartServer();
		}

		protected override void SpawnMachineAsInstalled()
		{
			base.SpawnMachineAsInstalled();

			chargePressure = CHARGED_PRESSURE;
			SetBinState(BinState.Ready);
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (virtualContainer != null) Despawn.ServerSingle(virtualContainer.gameObject);
		}

		#endregion Lifecycle

		#region Sync

		void OnSyncBinState(BinState oldState, BinState newState)
		{
			binState = newState;

			if (BinCharging)
			{
				rechargeSFX.Play();
			}
			else
			{
				rechargeSFX.Stop();
			}
		}

		#endregion Sync

		void SetBinState(BinState newState)
		{
			binState = newState;
			UpdateSpriteBinState();
		}

		#region Sprites

		protected override void UpdateSpriteConstructionState()
		{
			UpdateSpriteBinState();
		}

		void UpdateSpriteBinState()
		{
			if (MachineUnattached)
			{
				baseSpriteHandler.ChangeSprite((int) BinSprite.Sideways);
				overlaysSpriteHandler.PushClear();
				return;
			}

			switch (binState)
			{
				case BinState.Disconnected:
					baseSpriteHandler.ChangeSprite((int) BinSprite.Upright);
					overlaysSpriteHandler.PushClear();
					break;
				case BinState.Off:
					baseSpriteHandler.ChangeSprite((int) BinSprite.Upright);
					overlaysSpriteHandler.PushClear();
					break;
				case BinState.Ready:
					baseSpriteHandler.ChangeSprite((int) BinSprite.Upright);
					overlaysSpriteHandler.ChangeSprite((int) BinOverlaySprite.Ready);
					break;
				case BinState.Flushing:
					baseSpriteHandler.ChangeSprite((int) BinSprite.Flushing);
					overlaysSpriteHandler.PushClear();
					break;
				case BinState.Recharging:
					baseSpriteHandler.ChangeSprite((int) BinSprite.Upright);
					overlaysSpriteHandler.ChangeSprite((int) BinOverlaySprite.Charging);
					break;
			}

			BinStateUpdated?.Invoke();
		}

		#endregion Sprites

		#region Interactions

		// Click on disposal bin
		public override bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.HandObject == null) return false;

			if (base.WillInteract(interaction, side)) return true;
			// Bin accepts all items for disposal.
			else if (MachineSecured) return true;

			return false;
		}

		// Click on disposal bin
		public override void ServerPerformInteraction(PositionalHandApply interaction)
		{
			currentInteraction = interaction;

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench)
					&& MachineWrenchable) TryUseWrench();
			else if (Validations.HasUsedActiveWelder(interaction)
					&& MachineWeldable) TryUseWelder();
			else if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Screwdriver)
					&& Screwdriverable) TryUseScrewdriver();

			else if (MachineSecured) StoreItem();
		}

		// Drag something and drop on disposal bin
		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (!Validations.IsInReach(
					interaction.Performer.RegisterTile(),
					interaction.UsedObject.RegisterTile(),
					side == NetworkSide.Server)) return false;

			return true;
		}

		// Drag something and drop on disposal bin
		public void ServerPerformInteraction(MouseDrop interaction)
		{
			if (interaction.UsedObject == null) return;
			if (!interaction.UsedObject.TryGetComponent<PlayerScript>(out var script)) return; // Test to see if player

			// Don't store player unless secured.
			if (!MachineSecured) return;
			StartStoringPlayer(interaction);
		}

		public override string Examine(Vector3 worldPos = default)
		{
			string baseString = "It";
			if (FloorPlatingExposed()) baseString = base.Examine().TrimEnd('.') + " and";

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

		public void PlayerTryClimbingOut(GameObject player)
		{
			if (BinFlushing) return;

			if (player.TryGetComponent(out ObjectBehaviour playerBehaviour))
			{
				EjectPlayer(playerBehaviour);
			}
		}

		#endregion Interactions

		void StoreItem()
		{
			if (virtualContainer == null) virtualContainer = SpawnNewContainer();
			Inventory.ServerDrop(currentInteraction.HandSlot, currentInteraction.TargetVector);
			virtualContainer.AddItem(currentInteraction.HandObject.GetComponent<ObjectBehaviour>());

			this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
		}

		// TODO This was copied from somewhere. Where?
		void StartStoringPlayer(MouseDrop interaction)
		{
			List<LayerType> excludeObjects = new List<LayerType>() { LayerType.Objects };
			Vector3Int targetObjectLocalPosition = interaction.TargetObject.RegisterTile().LocalPosition;
			Vector3Int targetObjectWorldPos = interaction.TargetObject.WorldPosServer().CutToInt();

			if (!interaction.UsedObject.RegisterTile().Matrix.IsPassableAt(targetObjectLocalPosition, true, excludeLayers: excludeObjects))
			{
				return;
			}

			void StoringPlayer()
			{
				PlayerScript playerScript;
				if (interaction.UsedObject.TryGetComponent(out playerScript))
				{
					if (playerScript.registerTile.Matrix.IsPassableAt(targetObjectLocalPosition, true, excludeLayers: excludeObjects))
					{
						playerScript.PlayerSync.SetPosition(targetObjectWorldPos);
					}
				}
				else
				{
					var transformComp = interaction.UsedObject.GetComponent<CustomNetTransform>();
					if (transformComp != null)
					{
						transformComp.AppearAtPositionServer(targetObjectWorldPos);
					}
				}

				StorePlayer(interaction);
			}

			StandardProgressActionConfig cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction, false, false, false);
			StandardProgressAction.Create(cfg, StoringPlayer).ServerStartProgress(interaction.UsedObject.RegisterTile(), 2, interaction.Performer);
		}

		void StorePlayer(MouseDrop interaction)
		{
			if (virtualContainer == null) virtualContainer = SpawnNewContainer();
			virtualContainer.AddPlayer(interaction.DroppedObject.GetComponent<ObjectBehaviour>());

			this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
		}

		void EjectPlayer(ObjectBehaviour playerBehaviour)
		{
			if (virtualContainer == null) return;
			virtualContainer.RemovePlayer(playerBehaviour);
		}

		#region UI

		public void FlushContents()
		{
			if (BinReady) StartCoroutine(RunFlushSequence());
		}

		public void EjectContents()
		{
			if (autoFlushCoroutine != null) StopCoroutine(autoFlushCoroutine);
			if (BinFlushing) return;
			if (virtualContainer == null) return;

			Despawn.ServerSingle(virtualContainer.gameObject);
			virtualContainer = null;
		}

		public void TogglePower()
		{
			if (PowerOff) TurnPowerOn();
			else TurnPowerOff();
		}

		#endregion UI

		void TurnPowerOn()
		{
			// Cannot turn the pump on if power is not connected.
			if (PowerDisconnected) return;

			if (BinCharged)
			{
				SetBinState(BinState.Ready);
				if (ServerHasContents)
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

		void TurnPowerOff()
		{
			// Cannot disable power while flushing
			if (BinFlushing) return;

			if (autoFlushCoroutine != null) StopCoroutine(autoFlushCoroutine);
			if (rechargeCoroutine != null) StopCoroutine(rechargeCoroutine);
			SetBinState(BinState.Off);
		}

		IEnumerator Recharge()
		{
			while (BinCharging && !BinCharged)
			{
				yield return WaitFor.Seconds(1);
				chargePressure += 20;
			}

			if (!PowerOff && !PowerDisconnected)
			{
				SetBinState(BinState.Ready);
				this.RestartCoroutine(AutoFlush(), ref autoFlushCoroutine);
			}

			// Sound of the bin's air intake flap closing.
			SoundManager.PlayNetworkedAtPos("Click", registerObject.WorldPositionServer, sourceObj: gameObject);
		}

		IEnumerator RunFlushSequence()
		{
			// Bin orifice closes...
			SetBinState(BinState.Flushing);
			yield return WaitFor.Seconds(ANIMATION_TIME);

			// Bin orifice closed. Release the charge.
			chargePressure = 0;
			SoundManager.PlayNetworkedAtPos("DisposalMachineFlush", registerObject.WorldPositionServer, sourceObj: gameObject);
			if (virtualContainer != null)
			{
				virtualContainer.GetComponent<ObjectBehaviour>().parentContainer = null;
				DisposalsManager.Instance.NewDisposal(virtualContainer);
				virtualContainer = null;
			}

			// Restore charge.
			SetBinState(BinState.Recharging);
			StartCoroutine(Recharge());
		}

		IEnumerator AutoFlush()
		{
			yield return WaitFor.Seconds(AUTO_FLUSH_DELAY);
			if (BinReady && ServerHasContents) StartCoroutine(RunFlushSequence());
		}

		#region Construction

		void TryUseScrewdriver()
		{
			// Assume binState is Secured
			string finishPerformerMsg = $"You connect the {objectAttributes.InitialName} to the power.";
			string finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} connects the " +
						$"{objectAttributes.InitialName} to the power.";

			if (!PowerDisconnected)
			{
				finishPerformerMsg = $"You disconnect the {objectAttributes.InitialName} from the power.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} disconnects the " +
						$"{objectAttributes.InitialName} from the power.";
			}

			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, 0, "", "", finishPerformerMsg, finishOthersMsg, () => UseScrewdriver());
		}

		void UseScrewdriver()
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
	}
}
