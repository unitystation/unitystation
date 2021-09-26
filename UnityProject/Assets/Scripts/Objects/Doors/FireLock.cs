using Objects.Wallmounts;
using Systems.ObjectConnection;
using UnityEngine;

namespace Doors
{
	public class FireLock : InteractableDoor, IMultitoolSlaveable
	{
		public FireAlarm fireAlarm;

		public override void TryClose() { }

		public override void TryOpen(GameObject performer) { }

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

		#endregion
	}
}
