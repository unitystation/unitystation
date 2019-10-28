using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StorageHandler : MonoBehaviour
{
	private GameObject inventorySlotPrefab;
	/// <summary>
	/// Currently opened StorageObj (like the backpack that's currently being looked in)
	/// </summary>
	public StorageObject currentOpenStorage {get; private set;}
	// holds the currently rendered ui slots linked to the open storage.
	private List<UI_ItemSlot> currentOpenStorageUISlots = new List<UI_ItemSlot>();

	void Awake()
	{
		inventorySlotPrefab = Resources.Load("InventorySlot")as GameObject;
	}

	public void OpenStorageUI(StorageObject storageObj)
	{
		currentOpenStorage = storageObj;
		currentOpenStorage.clientUpdatedDelegate += StorageUpdatedEvent;
		PopulateInventorySlots();
		SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);
	}

	private void PopulateInventorySlots()
	{
		for (int i = 0; i < currentOpenStorage.inventorySlotList.Count; i++)
		{
			GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
			newSlot.transform.parent = transform;
			newSlot.transform.localScale = Vector3.one;
			var uiItemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
			uiItemSlot.UpdateFromStorage(i, currentOpenStorage);
			currentOpenStorageUISlots.Add(uiItemSlot);

		}
	}

	public void CloseStorageUI()
	{
		SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);

		currentOpenStorage.clientUpdatedDelegate -= StorageUpdatedEvent;
		currentOpenStorage = null;

		foreach (var uiItemSlot in currentOpenStorageUISlots)
		{
			InventorySlotCache.Remove(uiItemSlot);
			Destroy(uiItemSlot.transform.parent.gameObject);
		}
		currentOpenStorageUISlots.Clear();
	}

	private void StorageUpdatedEvent()
	{
		Logger.Log("Storage updated while open" , Category.Inventory);
		//relink all the slots based on the current inventory slots in the open storage object
		for (int i = 0; i < currentOpenStorage.inventorySlotList.Count; i++)
		{
			var uiItemSlot = currentOpenStorageUISlots[i];
			uiItemSlot.UpdateFromStorage(i, currentOpenStorage);
		}
	}
}