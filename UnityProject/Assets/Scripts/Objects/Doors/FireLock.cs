using System;
using Objects.Wallmounts;
using Systems.ObjectConnection;
using Messages.Server;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;

namespace Doors
{
	public class FireLock : MonoBehaviour, IMultitoolSlaveable
	{
		public FireAlarm fireAlarm;

		private DoorMasterController doorMasterController;
		public DoorMasterController DoorMasterController => doorMasterController;

		private void Awake()
		{
			doorMasterController = GetComponent<DoorMasterController>();
		}

		public void ReceiveAlert()
		{
			doorMasterController.TryClose();
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

		#endregion
	}
}
