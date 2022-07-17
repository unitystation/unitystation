using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using Objects.Research;
using UI.Core.Net.Elements.Dynamic.Spawned;
using UI.Core.NetUI;
using UnityEngine;

public class GUI_TeleporterConsole : NetTab
{
	[SerializeField]
	private BeaconList beaconList = null;

	private TeleporterControl teleporterControl;

	public override void OnEnable()
	{
		base.OnEnable();
		OnTabOpened.AddListener(PlayerJoinsTab);
	}

	private void OnDisable()
	{
		OnTabOpened.RemoveListener(PlayerJoinsTab);
	}

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	private IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			// waiting for Provider
			yield return WaitFor.EndOfFrame;
		}

		teleporterControl = Provider.GetComponent<TeleporterControl>();
	}

	private void PlayerJoinsTab(PlayerInfo newPeeper = default)
	{
		var beacons = TrackingBeacon.GetAllBeaconOfType(teleporterControl.TrackingBeaconType);

		//Clear and set all beacons again, there probably is a better way to do this.
		beaconList.SetBeacons(beacons);
	}

	public void OnTeleporterEntryButtonPressed(TeleporterEntry.TeleporterBeaconData teleporterBeaconData, PlayerInfo player)
	{
		teleporterControl.SetNewBeacon(teleporterBeaconData.TrackingBeacon);

		PlayClick();
	}
}
