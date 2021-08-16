using UnityEngine;
using Systems.ObjectConnection;
using Objects.Wallmounts;

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

		public MultitoolConnectionType ConType => MultitoolConnectionType.FireAlarm;

		bool IMultitoolSlaveable.IsLinked => fireAlarm != null;

		public void SetMaster(IMultitoolMasterable Imaster)
		{
			FireAlarm newFireAlarm = (Imaster as Component)?.gameObject.GetComponent<FireAlarm>();
			if (newFireAlarm == null) return; // Might try to add firelock to something that is not a firealarm e.g. APC

			if (fireAlarm != null)
			{
				fireAlarm.FireLockList.Remove(this);
			}

			fireAlarm = newFireAlarm;
			fireAlarm.FireLockList.Add(this);
		}

		#endregion

		//Copied over from LightSource.cs
		void OnDrawGizmosSelected()
		{
			var sprite = GetComponentInChildren<SpriteRenderer>();
			if (sprite == null)
				return;
			if (fireAlarm == null)
				return;
			//Highlight associated fireAlarm.
			Gizmos.color = new Color(1, 0.5f, 0, 1);
			Gizmos.DrawLine(fireAlarm.transform.position, gameObject.transform.position);
			Gizmos.DrawSphere(fireAlarm.transform.position, 0.25f);
		}
	}
}
