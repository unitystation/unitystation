using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles displaying the contents of an item storage, such as another player's inventory or a backpack.
/// </summary>
public class UI_StorageHandler : MonoBehaviour
{
	[Tooltip("Button which should close the storage UI. Will be positioned / made visible when" +
	         " the UI is opened and made invisible when it is closed.")]
	[SerializeField]
	private GameObject closeStorageUIButton = null;
	private GameObject inventorySlotPrefab;

	[Tooltip("GameObject under which all the other player UI slots live (for showing another player's inventory)")]
	[SerializeField]
	private GameObject otherPlayerStorage = null;
	private UI_ItemSlot[] otherPlayerSlots;



	/// <summary>
	/// Currently opened ItemStorage (like the backpack that's currently being looked in)
	/// </summary>
	public ItemStorage CurrentOpenStorage {get; private set;}
	// holds the currently rendered ui slots linked to the open storage.
	private List<UI_ItemSlot> currentOpenStorageUISlots = new List<UI_ItemSlot>();

	void Awake()
	{
		inventorySlotPrefab = Resources.Load("InventorySlot")as GameObject;
		otherPlayerSlots = otherPlayerStorage.GetComponentsInChildren<UI_ItemSlot>();
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
			SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position, PlayerManager.LocalPlayer);
		}
	}

	private void PopulateInventorySlots()
	{
		//are we dealing with another player's storage or a simple indexed storage?
		if (CurrentOpenStorage.GetComponent<PlayerScript>() != null)
		{
			//player storage
			//turn it on and link all the slots
			foreach (var otherPlayerSlot in otherPlayerSlots)
			{
				otherPlayerSlot.LinkSlot(CurrentOpenStorage.GetNamedItemSlot(otherPlayerSlot.NamedSlot));
			}
			otherPlayerStorage.SetActive(true);
		}
		else
		{
			//indexed storage
			//create a slot element for each indexed slot in the storage
			for (int i = 0; i < CurrentOpenStorage.ItemStorageStructure.IndexedSlots; i++)
			{
				GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
				newSlot.transform.SetParent(transform);
				newSlot.transform.localScale = Vector3.one;
				var uiItemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();
				uiItemSlot.LinkSlot(CurrentOpenStorage.GetIndexedItemSlot(i));
				currentOpenStorageUISlots.Add(uiItemSlot);
			}
			closeStorageUIButton.transform.SetAsLastSibling();
			closeStorageUIButton.SetActive(true);
		}
	}

	/// <summary>
	/// Drops all the items other player has
	/// </summary>
	public void DropOtherPlayerAll()
	{
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdDisrobe(CurrentOpenStorage.gameObject);
	}

	public void CloseStorageUI()
	{
		if (PlayerManager.LocalPlayer != null)
		{
			SoundManager.PlayAtPosition("Rustle#", PlayerManager.LocalPlayer.transform.position, PlayerManager.LocalPlayer);
		}
		CurrentOpenStorage = null;
		otherPlayerStorage.SetActive(false);
		foreach (var uiItemSlot in currentOpenStorageUISlots)
		{
			Destroy(uiItemSlot.transform.parent.gameObject);
		}
		currentOpenStorageUISlots.Clear();
		closeStorageUIButton.SetActive(false);
	}
}
