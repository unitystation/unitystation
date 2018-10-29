using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class StorageObject : NetworkBehaviour
{
	//Had to split the storageSlot caches up because of syncing difficulty with Host players (server and client in one)
	[HideInInspector]
	public StorageSlots storageSlotsServer;
	[HideInInspector]
	public StorageSlots storageSlotsClient;
	public int maxSlots = 7;
	public ItemSize maxItemSize = ItemSize.Large;

	public Action clientUpdatedDelegate;

	public override void OnStartClient()
	{
		base.OnStartClient();
		StartCoroutine(InitSlots(false));
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		StartCoroutine(InitSlots(true));
	}

	IEnumerator InitSlots(bool _isServer)
	{
		//Wait for onscene change event to take place on InventoryManager
		yield return YieldHelper.EndOfFrame;

		storageSlotsServer = new StorageSlots();
		storageSlotsClient = new StorageSlots();
		for (int i = 0; i < maxSlots; i++)
		{
			InventorySlot invSlot = null;
			if (_isServer)
			{
				invSlot = new InventorySlot(System.Guid.NewGuid(), "inventory" + i);
				storageSlotsServer.inventorySlots.Add(invSlot);
			}
			else
			{
				invSlot = new InventorySlot(System.Guid.Empty, "inventory" + i);
				storageSlotsClient.inventorySlots.Add(invSlot);
			}
			
			InventoryManager.AddSlot(invSlot, _isServer);

		}

		if (_isServer)
		{
			RpcCurrentPlayerUUIDSync(GetUUIDJsonString());
		}
	}

	[Server]
	public void SyncUUIDsWithPlayer(GameObject recipient)
	{
		StorageObjectUUIDSyncMessage.Send(recipient, gameObject, GetUUIDJsonString());
	}

	[Server]
	private string GetUUIDJsonString()
	{
		var syncData = new StorageSlotsUUIDSync();
		for (int i = 0; i < storageSlotsServer.inventorySlots.Count; i++)
		{
			Debug.Log("Gather storageSLot UUID: " + storageSlotsServer.inventorySlots[i].UUID);
			syncData.UUIDs.Add(storageSlotsServer.inventorySlots[i].UUID);
		}
		
		return JsonUtility.ToJson(syncData);
	}

	//Syncing the UUID's of the slots with current players
	[ClientRpc]
	public void RpcCurrentPlayerUUIDSync(string data)
	{
		SyncUUIDs(data);
	}

	public void SyncUUIDs(string data)
	{
		var syncData = JsonUtility.FromJson<StorageSlotsUUIDSync>(data);
		Debug.Log("Start sync of: " + gameObject.name + " forServer? " + isServer);
		for (int i = 0; i < syncData.UUIDs.Count; i++)
		{
			Debug.Log("Update slot from server: " + syncData.UUIDs[i] + " from client: " + storageSlotsClient.inventorySlots[i].UUID + " - " + storageSlotsClient.inventorySlots[i].SlotName);

			storageSlotsClient.inventorySlots[i].UUID = syncData.UUIDs[i];
		}
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		StorageObjectSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(storageSlotsServer));
	}

	public void RefreshStorageItems(string data)
	{
		JsonUtility.FromJsonOverwrite(data, storageSlotsClient);
		RefreshInstanceIds();
	}

	private void RefreshInstanceIds()
	{
		for (int i = 0; i < storageSlotsClient.inventorySlots.Count; i++)
		{
			storageSlotsClient.inventorySlots[i].RefreshInstanceIdFromIdentifier();
		}
		if (clientUpdatedDelegate != null)
		{
			clientUpdatedDelegate.Invoke();
		}
	}

	public InventorySlot NextSpareSlot()
	{
		InventorySlot invSlot = null;

		for (int i = 0; i < storageSlotsClient.inventorySlots.Count; i++)
		{
			if (storageSlotsClient.inventorySlots[i].Item == null)
			{
				return storageSlotsClient.inventorySlots[i];
			}
		}

		return invSlot;
	}
}

[Serializable]
public class StorageSlots
{
	public List<InventorySlot> inventorySlots = new List<InventorySlot>();
}

[Serializable]
public class StorageSlotsUUIDSync
{
	public List<string> UUIDs = new List<string>();
}