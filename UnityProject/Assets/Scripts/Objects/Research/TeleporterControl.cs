using Items;
using UI.Core.Net;
using UnityEngine;

namespace Objects.Research
{
	/// <summary>
	/// Teleporter console
	/// </summary>
	public class TeleporterControl : TeleporterBase, IServerSpawn, ICanOpenNetTab
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

			SetBeacon(newBeacon);

			connectedHub.SetBeacon(newBeacon);
		}

		public bool CanOpenNetTab(GameObject playerObject, NetTabType netTabType)
		{
			if (connectedHub == null || connectedStation == null)
			{
				Chat.AddExamineMsg(playerObject, "Teleporter not fully set up");
				return false;
			}

			return true;
		}
	}
}