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
	public NetworkInstanceId ItemInstanceId = NetworkInstanceId.Invalid;
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
				ItemInstanceId = value.GetComponent<NetworkIdentity>().netId;
			}
			else
			{
				ItemInstanceId = NetworkInstanceId.Invalid;
			}
		}
	}

	public InventorySlot(Guid uuid, string slotName = "", bool isUISlot = false)
	{
		UUID = uuid.ToString();
		SlotName = slotName;
		IsUISlot = isUISlot;
	}
}