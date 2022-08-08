using Objects.Wallmounts;
using Shared.Systems.ObjectConnection;
using UnityEngine;

namespace Doors
{
	public class FireLock : MonoBehaviour, IMultitoolSlaveable
	{
		public FireAlarm fireAlarm;

		private RegisterTile registerTile;

		private DoorMasterController doorMasterController;
		public DoorMasterController DoorMasterController => doorMasterController;

		private void Awake()
		{
			doorMasterController = GetComponent<DoorMasterController>();
			registerTile = GetComponent<RegisterTile>();
		}

		public void ReceiveAlert()
		{
			doorMasterController.TryForceClose();

			if(doorMasterController.IsClosed == false) return;

			//registerTile.SetNewSortingOrder(SortingLayer.NameToID("Door Closed"));
		}

		#region Multitool Interaction

		MultitoolConnectionType IMultitoolLinkable.ConType => MultitoolConnectionType.FireAlarm;
		IMultitoolMasterable IMultitoolSlaveable.Master => fireAlarm;
		bool IMultitoolSlaveable.RequireLink => true;
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

		#endregion
	}
}
