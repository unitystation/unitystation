using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class InventorySlot
{
	public string UUID;
	public string SlotName = "";
	public bool IsUISlot = false;
	[NonSerialized]
	public NetworkInstanceId ItemInstanceId = NetworkInstanceId.Invalid; //Cannot add to any json data, use uint instead
	public uint netInstanceIdentifier; //serialized for json

	public PlayerScript Owner { get; set; } //null = no owner (only UI slots have owners)
	private GameObject item;
	public GameObject Item
	{
		get
		{
			if (item == null && ItemInstanceId != NetworkInstanceId.Invalid)
			{
				item = ClientScene.FindLocalObject(ItemInstanceId);
			}
			else if (item != null && ItemInstanceId == NetworkInstanceId.Invalid)
			{
				item = null;
			}
			return item;
		}
		set
		{
			if (value != null)
			{
				var netID = value.GetComponent<NetworkIdentity>().netId;
				ItemInstanceId = netID;
				netInstanceIdentifier = netID.Value;
			}
			else
			{
				ItemInstanceId = NetworkInstanceId.Invalid;
				netInstanceIdentifier = 0;
			}
		}
	}

	public InventorySlot(Guid uuid, string slotName = "", bool isUISlot = false, PlayerScript owner = null)
	{
		UUID = uuid.ToString();
		SlotName = slotName;
		IsUISlot = isUISlot;
		Owner = owner;
	}

	//For client only syncing:
	public void RefreshInstanceIdFromIdentifier()
	{
		if (netInstanceIdentifier == 0)
		{
			ItemInstanceId = NetworkInstanceId.Invalid;
		}
		else
		{
			ItemInstanceId = new NetworkInstanceId(netInstanceIdentifier);
		}
	}
}