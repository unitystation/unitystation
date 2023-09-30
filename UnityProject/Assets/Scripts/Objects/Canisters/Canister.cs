using System;
using Logs;
using UnityEngine;
using Mirror;
using Systems.Atmospherics;
using Systems.Shuttles;
using UI.Core.NetUI;

namespace Objects.Atmospherics
{
	/// <summary>
	/// Main component for canister.
	/// </summary>
	[RequireComponent(typeof(Integrity))]
	public class Canister : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable, IServerSpawn
	{
		public const float MAX_RELEASE_PRESSURE = AtmosConstants.ONE_ATMOSPHERE * 50;
		private const int BURST_SPRITE = 1;

		[Header("Canister GUI Settings")]
		[Tooltip("Name to show for the contents of this canister in the GUI")]
		public string ContentsName;
		[Tooltip("Tint of the main background in the GUI")]
		public Color UIBGTint;
		[Tooltip("Tint of the inner panel in the GUI")]
		public Color UIInnerPanelTint;

		public bool AcceptTanks = true;

		[Header("Canister Settings")]
		[Tooltip("What tier this canister is. Sets the pressure to 4500e[tier].")]
		[SerializeField] [Range(0, 3)]
		private int canisterTier = 0;

		[Tooltip("Whether this canister is considered open at roundstart.")]
		[SerializeField]
		private bool valveIsInitiallyOpen = false;

		[Header("Canister SpriteHandlers")]
		[SerializeField] private SpriteHandler baseSpriteHandler = default;
		[SerializeField] private SpriteHandler canisterTierOverlay = default;
		[SerializeField] private SpriteHandler pressureIndicatorOverlay = default;
		[SerializeField] private SpriteHandler connectorHoseOverlay = default;
		[SerializeField] private SpriteHandler tankInsertedOverlay = default;
		[SerializeField] private SpriteHandler openValveOverlay = default;

		// Components attached to GameObject.
		public GasContainer GasContainer { get; private set; }
		private RegisterObject registerObject;
		private UniversalObjectPhysics objectPhysics;
		private HasNetworkTab networkTab;

		private Connector connector;
		private ShuttleFuelConnector connectorFuel;

		public bool ValveIsOpen { get; private set; }
		public bool tankValveOpen;
		public GameObject InsertedContainer { get; private set; }
		public bool HasContainerInserted => InsertedContainer != null;
		public bool IsConnected => connector != null || connectorFuel != null || connectorFuel != null;

#pragma warning disable CS0414 // The boolean is used to trigger code on the clients.
		[SyncVar(hook = nameof(SyncBurstState))]
		private bool hasBurst = false;
#pragma warning restore CS0414

		/// <summary>
		/// Invoked on server side when connection status changes, provides a bool indicating
		/// if it is connected.
		///
		/// NOTE: Doesn't need to be server side since isConnected is a sync var (and thus
		/// is available to the client), but I'm not sure
		/// it's always going to be a syncvar so I'm making this hook server only.
		/// </summary>
		/// <returns></returns>
		[NonSerialized] public BoolEvent ServerOnConnectionStatusChange = new BoolEvent();

		[NonSerialized] public BoolEvent ServerOnExternalTankInserted = new BoolEvent();

		private HandApply interaction;

		private enum PressureIndicatorState
		{
			RedFlashing = 0,
			Red = 1,
			Orange = 2,
			OrangeYellow = 3,
			Yellow = 4,
			YellowGreen = 5,
			Green = 6
		}

		#region Lifecycle

		private void Awake()
		{
			GasContainer = GetComponent<GasContainer>();
			networkTab = GetComponent<HasNetworkTab>();
			registerObject = GetComponent<RegisterObject>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			// Update gas mix manually, in case Canister component loads before GasContainer.
			// This ensures pressure indicator and canister tier are set correctly.
			GasContainer.UpdateGasMix();

			SetCanisterTier();

			// We push pressureIndicatorOverlay ourselves; if not,
			// SpriteHandler will do so but overwrite the current SO when it loads after this component.
			pressureIndicatorOverlay.OrNull()?.PushTexture();
			RefreshOverlays();
			SetValve(valveIsInitiallyOpen);
			GasContainer.ServerContainerExplode += OnContainerExploded;
		}

		private void OnContainerExploded()
		{
			if (IsConnected)
			{
				DisconnectFromConnector();
			}

			if (HasContainerInserted)
			{
				EjectInsertedContainer();
			}

			baseSpriteHandler.ChangeSprite(BURST_SPRITE);
			if (canisterTier > 0) // Tier overlays only for above 0.
			{
				int burstTier = canisterTierOverlay.CataloguePage + (canisterTierOverlay.CatalogueCount / 2);
				canisterTierOverlay.ChangeSprite(burstTier);
			}
			pressureIndicatorOverlay.PushClear();
			connectorHoseOverlay.PushClear();
			tankInsertedOverlay.PushClear();
			openValveOverlay.PushClear();

			hasBurst = true;
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		private void SyncBurstState(bool oldState, bool newState)
		{
			if (newState)
			{
				registerObject.SetPassable(false, true);
				// After the canister bursts, we switch appropriate scripts.
				GetComponent<BurstCanister>().enabled = true;
				networkTab.enabled = false;
				// Disable this canister script in favour of the BurstCanister script.
				enabled = false;
			}
		}

		#endregion Lifecycle

		/// <summary>
		/// Set the state of the canister's valve. Will release contents to environment if
		/// the canister is not attached to a connector.
		/// </summary>
		/// <param name="isOpen">If on, gas can be added or removed from the canister.</param>
		public void SetValve(bool isOpen)
		{
			ValveIsOpen = isOpen;
			RefreshOverlays();
			if (isOpen)
			{
				UpdateManager.Add(UpdateMe, 1);
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
			}
		}

		/// <summary>
		/// Disconnects the canister from any connectors it is attached to.
		/// </summary>
		public void DisconnectFromConnector()
		{
			if (connector != null)
			{
				connector.DisconnectCanister();
				connector = null;
			}
			if (connectorFuel != null)
			{
				connectorFuel.DisconnectCanister();
				connectorFuel = null;
			}
		}

		/// <summary>
		/// Respawns the modified container back into the world - or into the player's hand, if possible.
		/// </summary>
		public void RetrieveInsertedContainer()
		{
			var gasContainer = InsertedContainer;
			EjectInsertedContainer();

			var playerScript = networkTab.LastInteractedPlayer().GetComponent<PlayerScript>();
			var bestHand = playerScript.DynamicItemStorage.GetBestHand();
			if (bestHand != null)
			{
				Inventory.ServerAdd(gasContainer, bestHand);
			}
		}

		private void EjectInsertedContainer()
		{
			InsertedContainer.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(gameObject.AssumedWorldPosServer());
			InsertedContainer = null;
			ServerOnExternalTankInserted.Invoke(false);
			RefreshOverlays();
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			return $"It is {(IsConnected ? "connected" : "not connected")} to a connector, " +
					$"there is {(HasContainerInserted ? "a" : "no")} container inserted and " +
					$"the valve is {(ValveIsOpen ? "open" : "closed")}.";
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//using wrench
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench)) return true;
			//using any fillable gas container
			if (AcceptTanks && Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.CanisterFillable)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			this.interaction = interaction;

			//can click on the canister with a wrench to connect/disconnect it from a connector
			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				TryWrenching();
			}

			//can click on the canister with a refillable tank to insert the refillable tank into the canister
			if (AcceptTanks && Validations.HasItemTrait(interaction, CommonTraits.Instance.CanisterFillable))
			{
				TryInsertingContainer();
			}
		}




		private void TryWrenching()
		{
			if (IsConnected)
			{
				DisconnectFromConnector();
				UpdateConnected();
				return;
			}

			var firstConnector = registerObject.Matrix.GetFirst<Connector>(registerObject.LocalPositionServer, true);
			if (firstConnector != null)
			{
				connector = firstConnector;
				connector.ConnectCanister(this);
				UpdateConnected();
				return;
			}

			// TODO: ShuttleFuelConnector should eventually be removed or at least inherit from Connector.
			var firstFuelConnector = registerObject.Matrix.GetFirst<ShuttleFuelConnector>(registerObject.LocalPositionServer, true);
			if (firstFuelConnector != null)
			{
				connectorFuel = firstFuelConnector;
				connectorFuel.ConnectCanister(this);
				UpdateConnected();
			}
		}

		private void TryInsertingContainer()
		{
			// TODO: this should probably be converted to using ItemStorage at some point.

			// Don't insert a container if one is already present, lest we wipe out the previous container from existence.
			if (HasContainerInserted)
			{
				Chat.AddExamineMsg(interaction.Performer, "A tank is already inside this canister.");
				return;
			}

			//always null check... always...
			if (interaction.UsedObject.TryGetComponent(out GasContainer gasContainer))
			{
				//copy the containers properties over, delete the container from the player's hand
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You insert the {interaction.UsedObject.ExpensiveName()} into the canister.",
					$"{interaction.Performer.ExpensiveName()} inserts a tank into the {this.ContentsName} canister.");
				Inventory.ServerDrop(interaction.HandSlot);
				InsertedContainer = interaction.UsedObject;
				interaction.UsedObject.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();
				ServerOnExternalTankInserted.Invoke(true);
				RefreshOverlays();
			}
			else
			{
				Loggy.LogError(
						$"{interaction.Performer} tried inserting {interaction.UsedObject} into {gameObject}, " +
						$"but the tank didn't have a {nameof(GasContainer)} component associated with it. " +
						$"Something terrible has happened, or an item that should not has the CanisterFillable ItemTrait.",
						Category.Atmos
				);
			}
		}

		private void UpdateConnected()
		{
			ToolUtils.ServerPlayToolSound(interaction);
			RefreshOverlays();
			objectPhysics.SetIsNotPushable(IsConnected);
			ServerOnConnectionStatusChange.Invoke(IsConnected);
		}

		#endregion Interaction

		private void SetCanisterTier()
		{
			if (canisterTier > 0)
			{
				GasContainer.GasMix.MultiplyGas(Mathf.Pow(10, canisterTier));
				canisterTierOverlay.ChangeSprite(canisterTier - 1); // Tier 0 has no overlay.
			}
		}

		public void UpdateMe()
		{
			if(!IsConnected)
				GasContainer.VentContents();
			if (tankValveOpen)
				MergeCanisterAndTank();
			RefreshPressureIndicator();
		}

		public void MergeCanisterAndTank()
		{
			//Check for inserted tank
			if(HasContainerInserted == false) return;

			GasContainer canisterTank = GetComponent<GasContainer>();
			GasContainer externalTank = InsertedContainer.GetComponent<GasContainer>();
			GasMix canisterGas = canisterTank.GasMix;
			GasMix tankGas = externalTank.GasMix;
			canisterTank.GasMix = tankGas.MergeGasMix(canisterGas);
			externalTank.GasMix = tankGas;
		}

		public void RefreshPressureIndicator()
		{
			if (pressureIndicatorOverlay == null) return;
			var pressure = GasContainer.ServerInternalPressure;
			if (pressure >= 9100)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Green);
			}
			else if (pressure >= 40 * AtmosConstants.ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.YellowGreen);
			}
			else if (pressure >= 30 * AtmosConstants.ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Yellow);
			}
			else if (pressure >= 20 * AtmosConstants.ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.OrangeYellow);
			}
			else if (pressure >= 10 * AtmosConstants.ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Orange);
			}
			else if (pressure >= 5 * AtmosConstants.ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Red);
			}
			else if (pressure >= 10)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.RedFlashing);
			}
			else
			{
				pressureIndicatorOverlay.OrNull()?.PushClear();
			}
		}

		private void RefreshOverlays()
		{
			RefreshPressureIndicator();

			// We set present sprite SO here.
			// If present SO is set in editor, then the overlays show in editor.
			if (connectorHoseOverlay != null) connectorHoseOverlay.ChangeSprite(0);
			if (tankInsertedOverlay != null) tankInsertedOverlay.ChangeSprite(0);
			if (openValveOverlay != null) openValveOverlay.ChangeSprite(0);

			if (connectorHoseOverlay != null) connectorHoseOverlay.ToggleTexture(IsConnected);
			if (tankInsertedOverlay != null) tankInsertedOverlay.ToggleTexture(HasContainerInserted);
			if (openValveOverlay != null) openValveOverlay.ToggleTexture(ValveIsOpen);
		}
	}
}
