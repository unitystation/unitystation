using UnityEngine;
using US13.Items.Others;
using US13.Managers;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;
using Util;

namespace US13.UI.Items.SpellBook
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

			beaconNameLabel.MasterSetValue(trackingBeacon.OrNull()?.ItemAttributesV2.ArticleName ?? "Emergency Teleport");

			beaconSetButtonLabel.MasterSetValue(handTeleporter.HandTeleporter.linkedBeacon == trackingBeacon ?
				"Currently Set" : "Set");
		}
	}
}