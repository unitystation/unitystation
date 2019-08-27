using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class InventorySlot
{
	public EquipSlot equipSlot;
	public bool IsUISlot = false;
	[NonSerialized]
	public NetworkInstanceId ItemInstanceId = NetworkInstanceId.Invalid; //Cannot add to any json data, use uint instead
	public uint netInstanceIdentifier; //serialized for json

	public GameObject Owner;
	private GameObject item;

	public ItemAttributes ItemAttributes { get; private set; }

	public GameObject Item
	{
		get
		{
			if (item == null && ItemInstanceId != NetworkInstanceId.Invalid)
			{
				item = ClientScene.FindLocalObject(ItemInstanceId);
				ItemAttributes = item.GetComponent<ItemAttributes>();
			}
			else if (item != null && ItemInstanceId == NetworkInstanceId.Invalid)
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
				NetworkInstanceId netID = value.GetComponent<NetworkIdentity>().netId;
				ItemInstanceId = netID;
				netInstanceIdentifier = netID.Value;
				ItemAttributes = value.GetComponent<ItemAttributes>();
				value.BroadcastMessage("OnAddToInventorySlot", this, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				ItemInstanceId = NetworkInstanceId.Invalid;
				netInstanceIdentifier = 0;
				ItemAttributes = null;
			}
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
			ItemInstanceId = NetworkInstanceId.Invalid;
		}
		else
		{
			ItemInstanceId = new NetworkInstanceId(netInstanceIdentifier);
		}
	}
}