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
		var syncData = new StorageSlotsUUIDSync();
		storageSlots = new StorageSlots();
		for (int i = 0; i < maxSlots; i++)
		{
			InventorySlot invSlot = null;
			if (_isServer)
			{
				invSlot = new InventorySlot(System.Guid.NewGuid(), "inventory" + i);
				storageSlots.inventorySlots.Add(invSlot);
				syncData.UUIDs.Add(invSlot.UUID);
			}
			else
			{
				invSlot = new InventorySlot(System.Guid.Empty, "inventory" + i);
				storageSlots.inventorySlots.Add(invSlot);
			}
			
			InventoryManager.AddSlot(invSlot, _isServer);

		}

		yield return YieldHelper.DeciSecond;

		if (syncData.UUIDs.Count != 0)
		{
			StorageObjectUUIDSyncMessage.SendAll(gameObject, JsonUtility.ToJson(syncData));
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
		for (int i = 0; i < storageSlots.inventorySlots.Count; i++)
		{
			syncData.UUIDs.Add(storageSlots.inventorySlots[i].UUID);
		}
		
		return JsonUtility.ToJson(syncData);
	}

	public void SyncUUIDs(string data)
	{
		var syncData = JsonUtility.FromJson<StorageSlotsUUIDSync>(data);
		for (int i = 0; i < syncData.UUIDs.Count; i++)
		{
			storageSlots.inventorySlots[i].UUID = syncData.UUIDs[i];
		}
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		StorageObjectSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(storageSlots));
	}

	public void RefreshStorageItems(string data)
	{
		JsonUtility.FromJsonOverwrite(data, storageSlots);
		RefreshInstanceIds();
	}

	private void RefreshInstanceIds()
	{
		for (int i = 0; i < storageSlots.inventorySlots.Count; i++)
		{
			storageSlots.inventorySlots[i].RefreshInstanceIdFromIdentifier();
		}
		if (clientUpdatedDelegate != null)
		{
			clientUpdatedDelegate.Invoke();
		}
	}

	public InventorySlot NextSpareSlot()
	{
		InventorySlot invSlot = null;

		for (int i = 0; i < storageSlots.inventorySlots.Count; i++)
		{
			if (storageSlots.inventorySlots[i].Item == null)
			{
				return storageSlots.inventorySlots[i];
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