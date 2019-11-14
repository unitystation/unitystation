using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_StorageHandler : MonoBehaviour
{
	[Tooltip("Button which should close the storage UI. Will be positioned / made visible when" +
	         " the UI is opened and made invisible when it is closed.")]
	[SerializeField]
	private GameObject closeStorageUIButton;
	private GameObject inventorySlotPrefab;
	/// <summary>
	/// Currently opened ItemStorage (like the backpack that's currently being looked in)
	/// </summary>
	public ItemStorage CurrentOpenStorage {get; private set;}
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
		//only update if it's actually different
		if (CurrentOpenStorage != itemStorage)
		{
			CloseStorageUI();
			CurrentOpenStorage = itemStorage;
			PopulateInventorySlots();
			SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);
		}

	}

	private void PopulateInventorySlots()
	{
		//create a slot element for each indexed slot in the storage
		for (int i = 0; i < CurrentOpenStorage.ItemStorageStructure.IndexedSlots; i++)
		{
			GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
			newSlot.transform.parent = transform;
			newSlot.transform.localScale = Vector3.one;
			var uiItemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
			uiItemSlot.LinkSlot(CurrentOpenStorage.GetIndexedItemSlot(i));
			currentOpenStorageUISlots.Add(uiItemSlot);
		}
		closeStorageUIButton.SetActive(true);
	}

	public void CloseStorageUI()
	{
		SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position);
		CurrentOpenStorage = null;
		foreach (var uiItemSlot in currentOpenStorageUISlots)
		{
			Destroy(uiItemSlot.transform.parent.gameObject);
		}
		currentOpenStorageUISlots.Clear();
		closeStorageUIButton.transform.parent = transform.parent;
		closeStorageUIButton.SetActive(false);

	}
}