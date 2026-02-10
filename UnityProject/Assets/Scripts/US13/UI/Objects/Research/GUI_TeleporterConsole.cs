using System.Collections;
using UnityEngine;
using US13.Items.Others;
using US13.Managers;
using US13.Objects.Research;
using US13.UI.Core;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Research
{
	public class GUI_TeleporterConsole : NetTab
	{
		[SerializeField]
		private EmptyItemList beaconList = null;

		private TeleporterControl teleporterControl;
		public TeleporterControl TeleporterControl => teleporterControl;

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

			PlayerJoinsTab();
		}

		private void PlayerJoinsTab(PlayerInfo newPeeper = default)
		{
			if(teleporterControl == null) return;

			var beacons = TrackingBeacon.GetAllBeaconOfType(teleporterControl.TrackingBeaconType);
			if (beacons.Count != beaconList.Entries.Count)
			{
				beaconList.SetItems(beacons.Count);
			}

			for (int i = 0; i < beaconList.Entries.Count; i++)
			{
				DynamicEntry dynamicEntry = beaconList.Entries[i];
				var entry = dynamicEntry.GetComponent<TeleporterEntry>();
				entry.SetValues(this, beacons[i]);
			}
		}

		public void OnTeleporterEntryButtonPressed(TrackingBeacon trackingBeacon, PlayerInfo player)
		{
			if(trackingBeacon == teleporterControl.LinkedBeacon) return;

			teleporterControl.SetNewBeacon(trackingBeacon);

			PlayClick();

			PlayerJoinsTab();
		}
	}
}
