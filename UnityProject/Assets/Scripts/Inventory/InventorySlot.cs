using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class InventorySlot : MonoBehaviour
{
	public Guid UUID;
	public string SlotName = "";
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

	//New InventorySlot without any items on start
	public InventorySlot(Guid uuid, string slotName = "")
	{
		UUID = uuid;
		SlotName = slotName;
	}
}