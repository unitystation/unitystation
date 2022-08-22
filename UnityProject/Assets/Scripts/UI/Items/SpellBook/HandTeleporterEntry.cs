using Items;
using UI.Core.NetUI;
using UI.Items;
using UnityEngine;

namespace UI.SpellBook
{
	public class HandTeleporterEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label beaconNameLabel = null;

		[SerializeField]
		private NetText_label beaconSetButtonLabel = null;

		private TrackingBeacon trackingBeacon;
		private GUI_HandTeleporter handTeleporter;

		public void OnBeaconSetButtonPressed(PlayerInfo player)
		{
			if (handTeleporter == null) return;

			handTeleporter.OnTeleporterEntryButtonPressed(trackingBeacon, player);
		}

		public void SetValues(GUI_HandTeleporter handTeleporter, TrackingBeacon trackingBeacon)
		{
			this.handTeleporter = handTeleporter;
			this.trackingBeacon = trackingBeacon;

			beaconNameLabel.SetValueServer(trackingBeacon.OrNull()?.ItemAttributesV2.ArticleName ?? "Emergency Teleport");

			beaconSetButtonLabel.SetValueServer(handTeleporter.HandTeleporter.linkedBeacon == trackingBeacon ?
				"Currently Set" : "Set");
		}
	}
}