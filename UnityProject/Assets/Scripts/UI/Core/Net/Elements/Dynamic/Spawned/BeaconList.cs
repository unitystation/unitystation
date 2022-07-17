using System.Collections.Generic;
using Items;
using UI.Core.NetUI;
using UnityEngine;

namespace UI.Core.Net.Elements.Dynamic.Spawned
{
	public class BeaconList : NetUIDynamicList
	{
		public void SetBeacons(List<TrackingBeacon> trackingBeacons)
		{
			Clear();

			foreach (var beacon in trackingBeacons)
			{
				AddNewBeacon(beacon);
			}
		}

		public void AddNewBeacon(TrackingBeacon trackingBeacon)
		{
			for (int i = Entries.Length - 1; i >= 0; i--)
			{
				var entry = Entries[i] as TeleporterEntry.TeleporterBeaconData;
				if (!entry || entry.TrackingBeacon != trackingBeacon) continue;

				Logger.LogWarning($"BeaconList: {trackingBeacon.ItemAttributesV2.ArticleName} is already in list!", Category.NetUI);
				return;
			}

			var newEntry = Add() as TeleporterEntry.TeleporterBeaconData;

			if (!newEntry)
			{
				Logger.LogWarning($"BeaconList: Added {newEntry} is not an TeleporterBeaconData!", Category.NetUI);
				return;
			}

			newEntry.TrackingBeacon = trackingBeacon;
			newEntry.BeaconName = trackingBeacon.ItemAttributesV2.ArticleName;

			NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		}

		public void RemoveBeacon(TrackingBeacon trackingBeacon)
		{
			for (int i = Entries.Length - 1; i >= 0; i--)
			{
				var entry = Entries[i] as TeleporterEntry.TeleporterBeaconData;
				if(!entry || entry.TrackingBeacon != trackingBeacon) continue;

				Remove(entry.name);
			}

			NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		}
	}
}