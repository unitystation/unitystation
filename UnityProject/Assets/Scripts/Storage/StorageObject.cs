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

	IEnumerator InitSlots()
	{
		//Wait for onscene change event to take place on InventoryManager
		yield return YieldHelper.EndOfFrame;
		storageSlots = new StorageSlots();
		for (int i = 0; i < maxSlots; i++)
		{
			var invSlot = new InventorySlot(System.Guid.NewGuid(), "inventory" + i);
			storageSlots.inventorySlots.Add(invSlot);
			InventoryManager.AddSlot(invSlot, isServer);
		}

		if (isServer)
		{
			Debug.Log("HANDLE SYNC WITH ALL NEW PLAYERS");
			RpcInitClient(JsonUtility.ToJson(storageSlots));
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(InitSlots());
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		StartCoroutine(InitSlots());
	}

	[ClientRpc] //This just syncs the slots and UUIDs after server creates them
	public void RpcInitClient(string data)
	{
		Debug.Log("STORAGE ITEM DATA: " + data);
		JsonUtility.FromJsonOverwrite(data, storageSlots);
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		StorageObjectSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(storageSlots));
	}

	public void RefreshStorageItems(string data)
	{
		JsonUtility.FromJsonOverwrite(data, storageSlots);
	}
}

[Serializable]
public class StorageSlots
{
	public int slotCount => inventorySlots.Count;

	public List<InventorySlot> inventorySlots = new List<InventorySlot>();
}