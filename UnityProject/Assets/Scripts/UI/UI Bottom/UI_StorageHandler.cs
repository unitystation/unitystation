using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StorageHandler : MonoBehaviour
{
	private GameObject inventorySlotPrefab;
	/// <summary>
	/// Currently opened ItemStorage (like the backpack that's currently being looked in)
	/// </summary>
	public ItemStorage currentOpenStorage {get; private set;}
	// holds the currently rendered ui slots linked to the open storage.
	private List<UI_ItemSlot> currentOpenStorageUISlots = new List<UI_ItemSlot>();

	void Awake()
	{
		inventorySlotPrefab = Resources.Load("InventorySlot")as GameObject;
	}

	/// <summary>
	/// Pop up the UI for viewing this storage
	/// </summary>
	/// <param name="itemStorage"></param>
	public void OpenStorageUI(ItemStorage itemStorage)
	{
		currentOpenStorage = itemStorage;
		PopulateInventorySlots();
		SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);
	}

	private void PopulateInventorySlots()
	{
		//create a slot element for each indexed slot in the storage
		for (int i = 0; i < currentOpenStorage.ItemStorageStructure.IndexedSlots; i++)
		{
			GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
			newSlot.transform.parent = transform;
			newSlot.transform.localScale = Vector3.one;
			var uiItemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
			uiItemSlot.LinkSlot(currentOpenStorage.GetIndexedItemSlot(i));
			currentOpenStorageUISlots.Add(uiItemSlot);
		}
	}

	public void CloseStorageUI()
	{
		SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);
		currentOpenStorage = null;
		foreach (var uiItemSlot in currentOpenStorageUISlots)
		{
			Destroy(uiItemSlot.transform.parent.gameObject);
		}
		currentOpenStorageUISlots.Clear();
	}
}