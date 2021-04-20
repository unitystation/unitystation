using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Mirror;
using Systems.Electricity;
using HealthV2;

namespace Objects.Medical
{
	public class DNAScanner : ClosetControl, ICheckedInteractable<MouseDrop>, IAPCPowerable
	{
		public LivingHealthMasterBase occupant;
		public string statusString;

		public bool Powered => powered;
		[SyncVar(hook = nameof(SyncPowered))] private bool powered;
		// tracks whether we've recieved our first power update from electricity.
		// allows us to avoid syncing power when it is unchanged
		private bool powerInit;

		private enum ScannerState
		{
			OpenUnpowered,
			OpenPowered,
			ClosedUnpowered,
			ClosedPowered,
			ClosedPoweredWithOccupant
		}

		public Engineering.APC RelatedAPC;

		private CancellationTokenSource cancelOccupiedAnim = new CancellationTokenSource();

		public override void OnStartClient()
		{
			base.OnStartClient();
			SyncPowered(powered, powered);
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);
			statusString = "Ready to scan.";
			SyncPowered(powered, powered);
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
		}

		protected override void ServerHandleContentsOnStatusChange(bool willClose)
		{
			base.ServerHandleContentsOnStatusChange(willClose);
			if (ServerHeldPlayers.Any())
			{
				var mob = ServerHeldPlayers.First();
				occupant = mob.GetComponent<LivingHealthMasterBase>();
			}
			else
			{
				occupant = null;
			}
		}

		public bool WillInteract(MouseDrop interaction, NetworkSide side)
		{
			if (side == NetworkSide.Server && IsClosed)
				return false;
			if (!Validations.CanInteract(interaction.Performer, side))
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
			var objectBehaviour = drop.DroppedObject.GetComponent<ObjectBehaviour>();
			if (objectBehaviour)
			{
				ServerStorePlayer(objectBehaviour);
				ServerToggleClosed(true);
			}
		}

		protected override void UpdateSpritesOnStatusChange()
		{
			if (ClosetStatus == ClosetStatus.Open)
			{
				cancelOccupiedAnim.Cancel();
				if (!powered)
				{
					doorSpriteHandler.ChangeSprite((int) ScannerState.OpenUnpowered);
				}
				else
				{
					doorSpriteHandler.ChangeSprite((int) ScannerState.OpenPowered);
				}
			}
			else if (!powered)
			{
				cancelOccupiedAnim.Cancel();
				doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedUnpowered);
			}
			else if (ClosetStatus == ClosetStatus.Closed)
			{
				cancelOccupiedAnim.Cancel();
				doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedPowered);
			}
			else if (ClosetStatus == ClosetStatus.ClosedWithOccupant)
			{
				cancelOccupiedAnim = new CancellationTokenSource();
				if (gameObject != null && gameObject.activeInHierarchy)
				{
					doorSpriteHandler.ChangeSprite((int) ScannerState.ClosedPoweredWithOccupant);
				}
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
				if (IsLocked)
				{
					ServerToggleLocked(false);
				}
			}
			UpdateSpritesOnStatusChange();
		}

		#region IAPCPowerable

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			RelatedAPC = GetComponent<APCPoweredDevice>().RelatedAPC;
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
