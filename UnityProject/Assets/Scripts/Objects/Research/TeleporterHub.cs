using System.Collections.Generic;
using Systems.Electricity;
using Gateway;
using Items;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter gateway
	/// </summary>
	public class TeleporterHub : TeleporterBase, IEnterable
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
				return;
			}

			TransportUtility.TransportObjectAndPulled(objectToTeleport, linkedBeacon.CurrentBeaconPosition());
		}

		public override void SetBeacon(TrackingBeacon newBeacon)
		{
			calibrated = false;

			base.SetBeacon(newBeacon);
		}
	}
}
