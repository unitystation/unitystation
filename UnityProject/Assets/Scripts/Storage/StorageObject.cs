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

	public List<InventorySlot> inventorySlotList = new List<InventorySlot>();

	public Action clientUpdatedDelegate;

	public void Start()
	{
		InitSlots();
	}

	void InitSlots()
	{
		storageSlots = new StorageSlots();
		for (int i = 0; i < maxSlots; i++)
		{
			InventorySlot invSlot = new InventorySlot(EquipSlot.inventory01 + i, false, gameObject);
			storageSlots.inventorySlots.Add(invSlot);
		}
	}

	[Server]
	public void NotifyPlayer(GameObject recipient)
	{
		//Validate data and make sure it is being pulled from Server List (Fixes Host problems)
		StorageSlots slotsData = new StorageSlots();
		foreach (InventorySlot slot in storageSlots.inventorySlots)
		{
			slotsData.inventorySlots.Add(slot);
		}

		StorageObjectSyncMessage.Send(recipient, gameObject, JsonUtility.ToJson(slotsData));
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
			var invSlot = storageSlots.inventorySlots[i];
			invSlot.RefreshInstanceIdFromIdentifier();
			invSlot.Owner = gameObject;
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

	public void SetUpFromStorageObjectData(StorageObjectData Data)
	{
		maxItemSize = Data.maxItemSize;
		maxSlots = Data.maxSlots;
	}
}

[Serializable]
public class StorageSlots
{
	public List<InventorySlot> inventorySlots = new List<InventorySlot>();
}