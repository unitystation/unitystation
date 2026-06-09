using UnityEngine;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Items.Others;
using US13.UI.Core.Net;

namespace US13.Objects.Research
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