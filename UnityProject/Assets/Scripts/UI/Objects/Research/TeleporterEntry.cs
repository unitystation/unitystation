using Items;
using UI.Core.NetUI;
using UnityEngine;

public class TeleporterEntry : DynamicEntry
{
	[SerializeField]
	private NetLabel beaconNameLabel = null;
	[SerializeField]
	private NetLabel beaconSetButtonLabel = null;

	private TrackingBeacon trackingBeacon;
	private GUI_TeleporterConsole teleporterConsole;

	public void OnBeaconSetButtonPressed(PlayerInfo player)
	{
		if (trackingBeacon == null || teleporterConsole == null) return;

		teleporterConsole.OnTeleporterEntryButtonPressed(trackingBeacon, player);
	}

	public void SetValues(GUI_TeleporterConsole teleporterConsole, TrackingBeacon trackingBeacon)
	{
		this.teleporterConsole = teleporterConsole;
		this.trackingBeacon = trackingBeacon;

		beaconNameLabel.SetValueServer(trackingBeacon.ItemAttributesV2.ArticleName);

		beaconSetButtonLabel.SetValueServer(teleporterConsole.TeleporterControl.LinkedBeacon == trackingBeacon ?
			"Currently Set" : "Set");
	}
}
