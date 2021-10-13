using System;
using Objects.Wallmounts;
using Systems.ObjectConnection;
using Messages.Server;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

namespace Doors
{
	public class FireLock : InteractableDoor, IMultitoolSlaveable
	{
		public SortingGroup SortingGroup;

		public FireAlarm fireAlarm;

		[SyncVar(hook = nameof(SynchroniseLayerState))]
		public DoorUpdateType DoorState = DoorUpdateType.Open;

		public override void TryClose() { }

		public override void TryOpen(GameObject performer) { }

		public void Awake()
		{
			SortingGroup = this.GetComponent<SortingGroup>();
			Controller.OnDoorClose.AddListener(DoorClose);
			Controller.OnDoorOpen.AddListener(DoorOpen);
		}

		public void DoorClose()
		{
			SynchroniseLayerState(DoorState, DoorUpdateType.Close);
		}

		public void DoorOpen()
		{
			SynchroniseLayerState(DoorState, DoorUpdateType.Open);
		}

		void TriggerAlarm()
		{
			if (!Controller.IsWelded)
			{
				if (fireAlarm)
				{
					fireAlarm.SendCloseAlerts();
				}
				else
				{
					ReceiveAlert();
				}
			}
		}

		public void ReceiveAlert()
		{
			if (Controller == null)
				return;
			Controller.CloseSignal();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			var integrity = GetComponent<Integrity>();
			integrity.OnExposedEvent.AddListener(TriggerAlarm);
			RegisterTile registerTile = GetComponent<RegisterTile>();
			MetaDataLayer metaDataLayer = MatrixManager.AtPoint(registerTile.WorldPositionServer, true).MetaDataLayer;
			Controller.Open();
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.FireAlarm;
		IMultitoolMasterable IMultitoolSlaveable.Master => fireAlarm;
		bool IMultitoolSlaveable.RequireLink => true;
		bool IMultitoolSlaveable.TrySetMaster(PositionalHandApply interaction, IMultitoolMasterable master)
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
			// Disconnect link to currently connected fire alarm.
			if (fireAlarm != null)
			{
				fireAlarm.FireLockList.Remove(this);
				fireAlarm = null;
			}

			if (master is FireAlarm alarm)
			{
				fireAlarm = alarm;
				fireAlarm.FireLockList.Add(this);
			}
		}

		public void UpdateLayerClosed()
		{
			SortingGroup.sortingLayerID = SortingLayer.NameToID("Doors Closed");
		}

		public void UpdateLayerOpen()
		{
			SortingGroup.sortingLayerID = SortingLayer.NameToID("Machines");
		}

		public void SynchroniseLayerState(DoorUpdateType OldType, DoorUpdateType NewType)
		{
			DoorState = NewType;
			if (NewType == DoorUpdateType.Close)
			{
				UpdateLayerClosed();
			}
			else if (NewType == DoorUpdateType.Open)
			{
				UpdateLayerOpen();
			}
		}

		#endregion
	}
}
