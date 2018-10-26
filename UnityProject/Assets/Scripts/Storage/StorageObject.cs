using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StorageObject : NetworkBehaviour
{
	[HideInInspector]
	public StorageSlots storageSlots;
	public int maxSlots = 7;

	public Action clientUpdatedDelegate;

	public override void OnStartServer()
	{
		storageSlots = new StorageSlots();
		for (int i = 0; i < maxSlots; i++)
		{
			storageSlots.inventorySlots.Add(new InventorySlot(System.Guid.NewGuid(), "inventory" + i));
		}

		RpcInitClient(JsonUtility.ToJson(storageSlots));

		base.OnStartServer();
	}

	[ClientRpc] //This just syncs the slots and UUIDs after server creates them
	public void RpcInitClient(string data)
	{
		storageSlots = JsonUtility.FromJson<StorageSlots>(data);
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		StorageObjectSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(storageSlots));
	}

	public void RefreshStorageItems(string data){
		JsonUtility.FromJsonOverwrite(data, storageSlots);
	}
}

[Serializable]
public class StorageSlots
{
	public int slotCount => inventorySlots.Count;

	public List<InventorySlot> inventorySlots = new List<InventorySlot>();
}