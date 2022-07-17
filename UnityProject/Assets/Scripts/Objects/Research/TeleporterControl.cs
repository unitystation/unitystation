using Items;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter console
	/// </summary>
	public class TeleporterControl : TeleporterBase, IServerSpawn
	{
		[SerializeField]
		private TrackingBeacon.TrackingBeaconTypes trackingBeaconType = TrackingBeacon.TrackingBeaconTypes.Station;
		public TrackingBeacon.TrackingBeaconTypes TrackingBeaconType => trackingBeaconType;

		public void OnSpawnServer(SpawnInfo info)
		{
			SetControl(this);
		}

		public void SetNewBeacon(TrackingBeacon newBeacon)
		{
			if(connectedHub == null) return;

			connectedHub.SetBeacon(newBeacon);
		}
	}
}