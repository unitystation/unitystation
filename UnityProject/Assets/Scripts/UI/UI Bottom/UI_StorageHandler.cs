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
		if(localSlotCache.Count == storageCache.storageSlotsClient.inventorySlots.Count){
			return;
		}

		for (int i = 0; i < storageCache.storageSlotsClient.inventorySlots.Count; i++)
		{
			GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
			newSlot.transform.parent = transform;
			newSlot.transform.localScale = Vector3.one;
			var itemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
			itemSlot.eventName = "inventory" + i;
			itemSlot.inventorySlot = storageCache.storageSlotsClient.inventorySlots[i];
			storageCache.storageSlotsClient.inventorySlots[i].SlotName = itemSlot.eventName;
			localSlotCache.Add(itemSlot);
			InventorySlotCache.Add(itemSlot);
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
			InventorySlotCache.Remove(localSlotCache[i]);
			Destroy(localSlotCache[i].transform.parent.gameObject);
		}
		localSlotCache.Clear();
	}

	private void StorageUpdatedEvent()
	{
		Logger.Log("Storage updated while open" , Category.Inventory);
		for(int i = 0; i < localSlotCache.Count; i++){
			localSlotCache[i].SetItem(localSlotCache[i].Item);
		}
	}
}