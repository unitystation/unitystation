using System;
using System.Linq;
using System.Text;
using Mirror;
using SecureStuff;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.HealthV2.Living;
using US13.Objects.Closets;
using US13.Objects.Engineering;
using US13.Systems.Electricity.Interfaces;
using US13.UI.Systems.MainHUD.UI_Bottom;
using Util;

namespace US13.Objects.Medical
{
	public class DNAScanner : NetworkBehaviour, IServerSpawn, IAPCPowerable, IExaminable, IEscapable,
			ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>
	{
		[NonSerialized] public LivingHealthMasterBase occupant;
		[PlayModeOnly] public string statusString;

		public bool Powered => powered;
		[SyncVar(hook = nameof(SyncPowered))] private bool powered;
		// tracks whether we've recieved our first power update from electricity.
		// allows us to avoid syncing power when it is unchanged
		private bool powerInit;

		private enum ScannerState
		{
			ClosedUnpowered = 0,
			OpenUnpowered = 1,
			ClosedUnpoweredWithOccupant = 2,
			ClosedPowered = 3,
			OpenPowered = 4,
			ClosedPoweredWithOccupant = 5,
		}

		public Engineering.APC RelatedAPC => powerable.RelatedAPC;

		private ObjectContainer container;
		private ClosetControl closet;
		private APCPoweredDevice powerable;
		private SpriteHandler spriteHandler;

		private void Awake()
		{
			container = GetComponent<ObjectContainer>();
			closet = GetComponent<ClosetControl>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			powerable = GetComponent<APCPoweredDevice>();
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncPowered(powered, powered);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			statusString = "Ready to scan.";
			SyncPowered(powered, powered);
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (closet.IsOpen == false)
				return false;
			if (!Validations.CanInteract(interaction.PerformerPlayerScript, side))
				return false;
			if (!Validations.IsAdjacent(interaction.Performer, interaction.DroppedObject))
				return false;
			if (!Validations.IsAdjacent(interaction.Performer, gameObject))
				return false;
			if (interaction.Performer == interaction.DroppedObject)
				return false;
			return true;
		}

		public void ServerPerformInteraction(MouseDrop drop)
		{
			container.StoreObject(drop.DroppedObject);
			UpdateState();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return interaction.Intent != Intent.Harm;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (closet.IsLocked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
						$"The scanner's {gameObject.ExpensiveName()} door bolts refuse to budge!");
				return;
			}

			UpdateState();
		}

		private void UpdateState()
		{
			if (closet.IsOpen)
			{
				closet.SetDoor(ClosetControl.Door.Closed); // store items on tile
				container.GetStoredObjects().FirstOrDefault(obj => obj.TryGetComponent(out occupant));
			}
			else
			{
				occupant = null;
				closet.SetDoor(ClosetControl.Door.Opened); // release contents
			}

			UpdateSprites();
		}

		public string Examine(Vector3 worldPos = default)
		{
			var sb = new StringBuilder();
			if (closet.IsLocked)
			{
				sb.Append("It is locked closed.");
			}
			else
			{
				sb.Append(closet.IsOpen ? "It is open" : "It is closed");
			}

			sb.Append(powered ? "." : " and looks unpowered.");

			if (container.IsEmpty == false)
			{
				sb.Append(" There seems to be something inside.");
			}

			return sb.ToString();
		}

		private void UpdateSprites()
		{
			if (occupant != null)
			{
				spriteHandler.SetCatalogueIndexSprite((int) (powered
						? ScannerState.ClosedPoweredWithOccupant
						: ScannerState.ClosedUnpoweredWithOccupant));
				return;
			}

			if (powered)
			{
				spriteHandler.SetCatalogueIndexSprite((int) (closet.IsOpen ? ScannerState.OpenPowered : ScannerState.ClosedPowered));
			}
			else
			{
				spriteHandler.SetCatalogueIndexSprite((int) (closet.IsOpen ? ScannerState.OpenUnpowered : ScannerState.ClosedUnpowered));
			}
		}

		private void SyncPowered(bool oldValue, bool value)
		{
			// does nothing if power is unchanged and
			// we've already init'd
			if (powered == value && powerInit) return;

			powered = value;
			if (powered == false)
			{
				if (closet.IsLocked)
				{
					closet.SetLock(ClosetControl.Lock.Unlocked);
				}
			}
			UpdateSprites();
		}

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			occupant = null;
			closet.SetDoor(ClosetControl.Door.Opened);
			UpdateSprites();
		}

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			if (state == PowerState.Off || state == PowerState.LowVoltage)
			{
				SyncPowered(powered, false);
			}
			else
			{
				SyncPowered(powered, true);
			}

			if (powerInit == false)
			{
				powerInit = true;
			}
		}

		#endregion
	}
}
