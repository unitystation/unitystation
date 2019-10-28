using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[Serializable]
public class InventorySlot
{
	public EquipSlot equipSlot;
	public bool IsUISlot = false;
	[NonSerialized]
	public uint ItemInstanceId = NetId.Invalid; //Cannot add to any json data, use uint instead
	public uint netInstanceIdentifier; //serialized for json

	public GameObject Owner;
	private GameObject item;

	public ItemAttributes ItemAttributes { get; private set; }

	public GameObject Item
	{
		get
		{
			if (item == null && ItemInstanceId != NetId.Invalid)
			{
				item = ClientScene.FindLocalObject(ItemInstanceId);
				ItemAttributes = item.GetComponent<ItemAttributes>();
			}
			else if (item != null && ItemInstanceId == NetId.Invalid)
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
				netInstanceIdentifier = netID;
				ItemAttributes = value.GetComponent<ItemAttributes>();
				value.BroadcastMessage("OnAddToInventorySlot", this, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				ItemInstanceId = NetId.Invalid;
				netInstanceIdentifier = 0;
				ItemAttributes = null;
			}

			item = value;
		}
	}

	public InventorySlot(EquipSlot newEquipSlot, bool isUISlot, GameObject owner = null)
	{
		equipSlot = newEquipSlot;
		IsUISlot = isUISlot;
		Owner = owner;
	}

	//For client only syncing:
	public void RefreshInstanceIdFromIdentifier()
	{
		if (netInstanceIdentifier == 0)
		{
			ItemInstanceId = NetId.Invalid;
		}
		else
		{
			ItemInstanceId = netInstanceIdentifier;
		}
	}
}