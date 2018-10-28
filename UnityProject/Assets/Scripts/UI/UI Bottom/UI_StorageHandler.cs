using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StorageHandler : MonoBehaviour
{
	private GameObject inventorySlotPrefab;
	public StorageObject storageCache {get; private set;}
	private List<UI_ItemSlot> localSlotCache = new List<UI_ItemSlot>();

	void Awake()
	{
		inventorySlotPrefab = Resources.Load("InventorySlot")as GameObject;
	}

	public void OpenStorageUI(StorageObject storageObj)
	{
		storageCache = storageObj;
		storageCache.clientUpdatedDelegate += StorageUpdatedEvent;
		PopulateInventorySlots();
	}

	private void PopulateInventorySlots()
	{
		if(localSlotCache.Count == storageCache.storageSlots.slotCount){
			return;
		}

		for (int i = 0; i < storageCache.storageSlots.slotCount; i++)
		{
			GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
			newSlot.transform.parent = transform;
			newSlot.transform.localScale = Vector3.one;
			var itemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
			itemSlot.eventName = "inventory" + i;
			itemSlot.inventorySlot = storageCache.storageSlots.inventorySlots[i];
			storageCache.storageSlots.inventorySlots[i].SlotName = itemSlot.eventName;
			localSlotCache.Add(itemSlot);
			UIManager.InventorySlots.Add(itemSlot);
			if(itemSlot.Item != null){
				itemSlot.SetItem(itemSlot.Item);
			}
		}
	}

	public void CloseStorageUI()
	{
		storageCache.clientUpdatedDelegate -= StorageUpdatedEvent;
		storageCache = null;

		for (int i = localSlotCache.Count - 1; i >= 0; i--)
		{
			UIManager.InventorySlots.Remove(localSlotCache[i]);
			Destroy(localSlotCache[i].transform.parent.gameObject);
		}
		localSlotCache.Clear();
	}

	private void StorageUpdatedEvent()
	{
		Debug.Log("Storage updated while open");
	}
}