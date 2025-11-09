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
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		private void Awake()
		{
			doorMasterController = GetComponent<DoorMasterController>();
			registerTile = GetComponent<RegisterTile>();
		}

		/// <summary>
		/// Run this when the fire alarm (or other device) sends the alert to close
		/// </summary>
		public void ReceiveAlert()
		{
			if (doorMasterController.HasPower == false) return;

			foreach (var door in MatrixManager.GetAt<DoorMasterController>(registerTile.WorldPositionServer, true))
			{
				if (door.IsFireLock)
					continue;

				//Close the door and THEN paralyze the door, otherwise the door will be stuck open under the firelock
				door.PulseTryClose(bypassSoftware: true);
				door.IsFireLockEngaged = true;
			}

			doorMasterController.PulseTryClose(bypassSoftware: true);
		}

		/// <summary>
		/// Run this when the fire alarm (or other device) sends the all clear notification
		/// </summary>
		public void ClearAlert()
		{
			if (doorMasterController.HasPower == false) return;
			
			foreach (var door in MatrixManager.GetAt<DoorMasterController>(registerTile.WorldPositionServer, true))
			{
				if (door.IsFireLock)
					continue;

				//Tell any doors that the firelock is lifted
				door.IsFireLockEngaged = false;
			}

			//Open the firelock
			doorMasterController.PulseTryOpen(bypassSoftware: true);
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
