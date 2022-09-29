using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using Items.Others;
using Objects.Research;
using UI.Core.NetUI;
using UI.SpellBook;
using UnityEngine;

namespace UI.Items
{
	public class GUI_HandTeleporter : NetTab
	{
			[SerializeField]
        	private EmptyItemList beaconList = null;

        	private HandTeleporter handTeleporter;
        	public HandTeleporter HandTeleporter => handTeleporter;

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

        		handTeleporter = Provider.GetComponent<HandTeleporter>();

        		PlayerJoinsTab();
        	}

        	private void PlayerJoinsTab(PlayerInfo newPeeper = default)
        	{
        		if(handTeleporter == null) return;

                var beacons = new List<TrackingBeacon>();

                //Null is the Emergency Teleport add first then other beacons
                beacons.Add(null);
                beacons= beacons.Concat(TrackingBeacon.GetAllBeaconOfType(handTeleporter.TrackingBeaconType)).ToList();
        		if (beacons.Count != beaconList.Entries.Count)
        		{
        			beaconList.SetItems(beacons.Count);
        		}

        		for (int i = 0; i < beaconList.Entries.Count; i++)
        		{
        			DynamicEntry dynamicEntry = beaconList.Entries[i];
        			var entry = dynamicEntry.GetComponent<HandTeleporterEntry>();
        			entry.SetValues(this, beacons[i]);
        		}
        	}

        	public void OnTeleporterEntryButtonPressed(TrackingBeacon trackingBeacon, PlayerInfo player)
            {
	            handTeleporter.linkedBeacon = trackingBeacon;

        		PlayerJoinsTab();
        	}
	}
}
