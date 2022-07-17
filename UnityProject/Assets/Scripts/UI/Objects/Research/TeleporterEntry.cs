using System.Collections;
using System.Collections.Generic;
using Items;
using UI.Core.NetUI;
using UI.Objects;
using UnityEngine;

public class TeleporterEntry : MonoBehaviour
{
	private GUI_TeleporterConsole teleporterConsole;

	[SerializeField]
	private NetLabel beaconNameLabel = null;
	[SerializeField]
	private NetLabel beaconSetButtonLabel = null;

	public TeleporterBeaconData teleporterBeaconData;

	public void OnBeaconSetButtonPressed(PlayerInfo player)
	{
		if (teleporterBeaconData == null || teleporterConsole == null) return;

		teleporterConsole.OnTeleporterEntryButtonPressed(teleporterBeaconData, player);
	}

	[System.Serializable]
	public class TeleporterBeaconData : DynamicEntry
	{
		//Server Only
		public TrackingBeacon TrackingBeacon;

		//For Clients
		public string BeaconName;
	}
}
