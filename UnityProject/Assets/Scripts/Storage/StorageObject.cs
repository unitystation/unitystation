using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

/// <summary>
/// Component which allows an object to store items (like a backpack)
/// </summary>
public class StorageObject : MonoBehaviour
{
	public int maxSlots = 7;
	public ItemSize maxItemSize = ItemSize.Large;

	public List<InventorySlot> inventorySlotList;

	public Action clientUpdatedDelegate;

	public void Awake()
	{
		InitSlots();
	}

	void InitSlots()
	{
		inventorySlotList = new List<InventorySlot>();
		for (int i = 0; i < maxSlots; i++)
		{
			InventorySlot invSlot = new InventorySlot(EquipSlot.inventory01 + i, false, gameObject);
			inventorySlotList.Add(invSlot);
		}
	}

	/// <summary>
	/// (server only) Update the given client's storage slots for this storage object, so the client now knows what's
	/// in this storage.
	/// </summary>
	/// <param name="recipient"></param>
	public void ServerNotifyPlayer(GameObject recipient)
	{
		StorageObjectSyncMessage.Send(recipient, this, inventorySlotList);
	}

	/// <summary>
	/// Updates this object's slots to be the specified slots.
	/// </summary>
	/// <param name="slots"></param>
	public void UpdateSlots(List<InventorySlot> slots)
	{
		this.inventorySlotList = slots;
		foreach (var slot in inventorySlotList)
		{
			slot.RefreshInstanceIdFromIdentifier();
			slot.Owner = gameObject;
		}

		clientUpdatedDelegate?.Invoke();
	}

	public InventorySlot NextSpareSlot()
	{
		foreach (var slot in inventorySlotList)
		{
			if (slot.Item == null)
			{
				return slot;
			}
		}

		return null;
	}

	public void SetUpFromStorageObjectData(StorageObjectData Data)
	{
		maxItemSize = Data.maxItemSize;
		maxSlots = Data.maxSlots;
	}

	public InventorySlot GetSlot(EquipSlot storeEquipSlot)
	{
		return inventorySlotList.FirstOrDefault(slot => slot.equipSlot == storeEquipSlot);
	}
}