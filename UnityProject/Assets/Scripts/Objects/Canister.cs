using System;
using UnityEngine;
using Mirror;
using Pipes;

namespace Objects.GasContainer
{
	/// <summary>
	/// Main component for canister
	/// </summary>
	[RequireComponent(typeof(Integrity))]
	public class Canister : NetworkBehaviour, ICheckedInteractable<HandApply>, IExaminable
	{
		private const float ONE_ATMOSPHERE = 101.325f; // kPa.
		public static readonly int MAX_RELEASE_PRESSURE = 4000;
		private const int BURST_SPRITE = 1;

		[Header("Canister GUI Settings")]
		[Tooltip("Name to show for the contents of this canister in the GUI")]
		public string ContentsName;
		[Tooltip("Tint of the main background in the GUI")]
		public Color UIBGTint;
		[Tooltip("Tint of the inner panel in the GUI")]
		public Color UIInnerPanelTint;

		[Header("Canister Settings")]
		[Tooltip("What tier this canister is. Will affect the capacity (only the overlay is implemented).")]
		[SerializeField] [Range(0, 3)] int canisterTier;

		[Header("Canister SpriteHandlers")]
		[SerializeField] private SpriteHandler baseSpriteHandler;
		[SerializeField] private SpriteHandler canisterTierOverlay;
		[SerializeField] private SpriteHandler pressureIndicatorOverlay;
		[SerializeField] private SpriteHandler connectorHoseOverlay;
		[SerializeField] private SpriteHandler tankInsertedOverlay;

		// Components attached to GameObject.
		public GasContainer GasContainer { get; private set; }
		private RegisterObject registerObject;
		private ObjectBehaviour objectBehaviour;
		private HasNetworkTab networkTab;

		private Connector connector;
		private ShuttleFuelConnector connectorFuel;

		public bool ValveIsOpen { get; private set; }
		public GameObject InsertedContainer { get; private set; }
		public bool HasContainerInserted => InsertedContainer != null;
		public bool IsConnected => connector != null || connectorFuel != null;

		[SyncVar(hook = nameof(SyncBurstState))]
		private bool hasBurst = false;

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
			Yellow = 2,
			Green = 3
		}

		#region Lifecycle

		private void Awake()
		{
			GasContainer = GetComponent<GasContainer>();
			networkTab = GetComponent<HasNetworkTab>();
			registerObject = GetComponent<RegisterObject>();
			objectBehaviour = GetComponent<ObjectBehaviour>();
			SetDefaultIntegrity();
		}

		private void Start()
		{
			SetCanisterTier();

			// We push pressureIndicatorOverlay ourselves; if not,
			// SpriteHandler will do so but overwrite the current SO when it loads after this component.
			pressureIndicatorOverlay.PushTexture();
			RefreshOverlays();
		}

		public override void OnStartServer()
		{
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

			hasBurst = true;
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshPressureIndicator);
		}

		//this is just here so anyone trying to change the armor value in inspector sees it being
		//reset
		private void OnValidate()
		{
			SetDefaultIntegrity();
		}

		private void SetDefaultIntegrity()
		{
			//default canister integrity values
			GetComponent<Integrity>().HeatResistance = 1000;
			GetComponent<Integrity>().Armor = new Armor
			{
				Melee = 50,
				Bullet = 50,
				Laser = 50,
				Energy = 100,
				Bomb = 10,
				Bio = 100,
				Rad = 100,
				Fire = 80,
				Acid = 50
			};
		}

		private void SyncBurstState(bool oldState, bool newState)
		{
			if (newState)
			{
				registerObject.Passable = true;
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
			if (!IsConnected)
			{
				GasContainer.SetVent(isOpen);
			}

			RefreshOverlays();

			if (isOpen)
			{
				UpdateManager.Add(RefreshPressureIndicator, 1);
			}
			else
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshPressureIndicator);
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
		/// Respawns the modified container back into the world
		/// </summary>
		public void RetrieveInsertedContainer()
		{
			EjectInsertedContainer();
			ItemStorage player = networkTab.LastInteractedPlayer().GetComponent<PlayerScript>().ItemStorage;
			HandInsert(player);
		}

		private void EjectInsertedContainer()
		{
			InsertedContainer.GetComponent<CustomNetTransform>().AppearAtPositionServer(gameObject.WorldPosServer());
			InsertedContainer = null;
			ServerOnExternalTankInserted.Invoke(false);
			RefreshOverlays();
		}

		/// <summary>
		/// Checks to see if it can put it in any hand, if it cant it will do nothing meaning the item should just drop.
		/// </summary>
		/// <param name="player"></param>
		private void HandInsert(ItemStorage player)
		{
			ItemSlot activeHand = player.GetActiveHandSlot();
			if (Inventory.ServerAdd(InsertedContainer, activeHand)) return;
			switch (activeHand.NamedSlot)
			{
				case NamedSlot.leftHand:
					ItemSlot rSlot = player.GetNamedItemSlot(NamedSlot.rightHand);
					Inventory.ServerAdd(InsertedContainer, rSlot);
					break;

				case NamedSlot.rightHand:
					ItemSlot lSlot = player.GetNamedItemSlot(NamedSlot.leftHand);
					Inventory.ServerAdd(InsertedContainer, lSlot);
					break;
			}
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
			if (!DefaultWillInteract.Default(interaction, side)) return false;

			//using wrench
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench)) return true;
			//using any fillable gas container
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.CanisterFillable)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			this.interaction = interaction;

			//can click on the canister with a wrench to connect/disconnect it from a connector
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench))
			{
				TryWrenching();
			}

			//can click on the canister with a refillable tank to insert the refillable tank into the canister
			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.CanisterFillable))
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
				interaction.UsedObject.GetComponent<CustomNetTransform>().DisappearFromWorldServer();
				ServerOnExternalTankInserted.Invoke(true);
				RefreshOverlays();
			}
			else
			{
				Logger.LogError(
						$"{interaction.Performer} tried inserting {interaction.UsedObject} into {gameObject}, " +
						$"but the tank didn't have a {nameof(GasContainer)} component associated with it. " +
						$"Something terrible has happened, or an item that should not has the CanisterFillable ItemTrait."
				);
			}
		}

		private void UpdateConnected()
		{
			ToolUtils.ServerPlayToolSound(interaction);
			RefreshOverlays();
			objectBehaviour.ServerSetPushable(!IsConnected);
			ServerOnConnectionStatusChange.Invoke(IsConnected);
		}

		#endregion Interaction

		public void RefreshPressureIndicator()
		{
			var pressure = GasContainer.ServerInternalPressure;
			if (pressure >= 40 * ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Green);
			}
			else if (pressure >= 10 * ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Yellow);
			}
			else if (pressure >= 5 * ONE_ATMOSPHERE)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.Red);
			}
			else if (pressure >= 10)
			{
				pressureIndicatorOverlay.ChangeSprite((int)PressureIndicatorState.RedFlashing);
			}
			else
			{
				pressureIndicatorOverlay.PushClear();
			}
		}

		private void SetCanisterTier()
		{
			if (canisterTier > 0)
			{
				canisterTierOverlay.PushTexture();
				canisterTierOverlay.ChangeSprite(canisterTier);
			}
		}

		private void RefreshOverlays()
		{
			// We set present sprite SO here.
			// If present SO is set in editor, then the SpriteRenderer is updated from blank too
			// - don't want overlays to show in editor.
			connectorHoseOverlay.ChangeSprite(0);
			tankInsertedOverlay.ChangeSprite(0);

			RefreshPressureIndicator();

			connectorHoseOverlay.ToggleTexture(IsConnected);
			tankInsertedOverlay.ToggleTexture(HasContainerInserted);
		}
	}
}
