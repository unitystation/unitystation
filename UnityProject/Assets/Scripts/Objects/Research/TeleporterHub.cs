using System.Collections.Generic;
using Systems.Electricity;
using Systems.Explosions;
using Gateway;
using Items;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter gateway
	/// </summary>
	public class TeleporterHub : TeleporterBase, IEnterable, IServerSpawn
	{
		private bool calibrated;

		public void OnStep(GameObject eventData)
		{
			if(powered == false) return;
			if(active == false) return;

			//Shouldn't occur
			if (linkedBeacon == null) return;

			InternalTeleport(eventData);
		}

		public bool WillStep(GameObject eventData)
		{
			return true;
		}

		private void InternalTeleport(GameObject objectToTeleport)
		{
			//TODO more uncalibrated accidents, e.g turn into fly people, mutate animals? (See IQuantumReaction)

			if (calibrated == false && objectToTeleport.TryGetComponent(out IQuantumReaction reaction))
			{
				reaction.OnTeleportStart();
				TransportUtility.TransportObjectAndPulled(objectToTeleport, linkedBeacon.CurrentBeaconPosition());
				reaction.OnTeleportEnd();

				SparkUtil.TrySpark(objectToTeleport, expose: false);
				return;
			}

			TransportUtility.TransportObjectAndPulled(objectToTeleport, linkedBeacon.CurrentBeaconPosition());
			SparkUtil.TrySpark(objectToTeleport, expose: false);
		}

		public override void SetBeacon(TrackingBeacon newBeacon)
		{
			calibrated = false;

			base.SetBeacon(newBeacon);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SetHub(this);
		}
	}
}
