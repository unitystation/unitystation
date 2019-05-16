using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[Serializable]
public class InventorySlot
{
	public string UUID;
	public string SlotName = "";
	public bool IsUISlot = false;
	[NonSerialized]
	public uint ItemInstanceId = uint.Invalid; //Cannot add to any json data, use uint instead
	public uint netInstanceIdentifier; //serialized for json

	public PlayerScript Owner { get; set; } //null = no owner (only UI slots have owners)
	private GameObject item;

	public ItemAttributes ItemAttributes { get; private set; }

	public GameObject Item
	{
		get
		{
			if (item == null && ItemInstanceId != uint.Invalid)
			{
				item = ClientScene.FindLocalObject(ItemInstanceId);
				ItemAttributes = item.GetComponent<ItemAttributes>();
			}
			else if (item != null && ItemInstanceId == uint.Invalid)
			{
				item = null;
				ItemAttributes = null;
			}

			return item;
		}
		set
		{
			if (value != null)
			{
				uint netID = value.GetComponent<NetworkIdentity>().netId;
				ItemInstanceId = netID;
				netInstanceIdentifier = netID.Value;
				ItemAttributes = value.GetComponent<ItemAttributes>();
				value.BroadcastMessage("OnAddToInventorySlot", this, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				ItemInstanceId = uint.Invalid;
				netInstanceIdentifier = 0;
				ItemAttributes = null;
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
			ItemInstanceId = uint.Invalid;
		}
		else
		{
			ItemInstanceId = new uint(netInstanceIdentifier);
		}
	}
}