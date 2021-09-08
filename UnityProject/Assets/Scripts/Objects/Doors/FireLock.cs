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
		IMultitoolMasterable IMultitoolSlaveable.Master { get => fireAlarm; set => SetMaster(value); }
		bool IMultitoolSlaveable.RequireLink => true;

		private void SetMaster(IMultitoolMasterable master)
		{
			FireAlarm newFireAlarm = (master as Component)?.gameObject.GetComponent<FireAlarm>();
			if (newFireAlarm == null) return; // Might try to add firelock to something that is not a firealarm e.g. APC

			if (fireAlarm != null)
			{
				fireAlarm.FireLockList.Remove(this);
			}

			fireAlarm = newFireAlarm;
			fireAlarm.FireLockList.Add(this);
		}

		#endregion
	}
}
