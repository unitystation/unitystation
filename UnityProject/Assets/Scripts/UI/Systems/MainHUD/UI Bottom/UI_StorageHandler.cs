using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Handles displaying the contents of an item storage, such as another player's inventory or a backpack.
	/// </summary>
	public class UI_StorageHandler : MonoBehaviour
	{
		[Tooltip("Button which should close the storage UI. Will be positioned / made visible when" +
		         " the UI is opened and made invisible when it is closed.")]
		[SerializeField]
		private GameObject closeStorageUIButton = null;

		[SerializeField] private GameObject inventorySlotPrefab = null;

		[SerializeField] private Text indexedStorageCapacity = default;

		/// <summary>
		/// Currently opened ItemStorage (like the backpack that's currently being looked in)
		/// </summary>
		public ItemStorage CurrentOpenStorage { get; private set; }

		// holds the currently rendered ui slots linked to the open storage.
		private readonly List<UI_ItemSlot> currentOpenStorageUISlots = new List<UI_ItemSlot>();

		/// <summary>
		/// Pop up the UI for viewing this storage
		/// </summary>
		/// <param name="itemStorage"></param>
		public void OpenStorageUI(ItemStorage itemStorage)
		{
			// only update if it's actually different
			if (CurrentOpenStorage != itemStorage)
			{
				CloseStorageUI();
				CurrentOpenStorage = itemStorage;
				PopulateInventorySlots();
			}
		}

		private void PopulateInventorySlots()
		{
			// indexed storage
			// create a slot element for each indexed slot in the storage
			var  indexedSlots = CurrentOpenStorage.GetItemSlots();
			int occupiedSlots = 0;
			foreach (var itemSlot in indexedSlots)
			{
				GameObject newSlot = Instantiate(inventorySlotPrefab, Vector3.zero, Quaternion.identity);
				newSlot.transform.SetParent(transform);
				newSlot.transform.localScale = Vector3.one;
				var uiItemSlot = newSlot.GetComponentInChildren<UI_ItemSlot>();

				if (itemSlot.IsOccupied)
					occupiedSlots++;

				uiItemSlot.LinkSlot(itemSlot);
				currentOpenStorageUISlots.Add(uiItemSlot);
				// listen for updates to update capacity
				uiItemSlot.ItemSlot.OnSlotContentsChangeClient.AddListener(OnSlotContentsChangeClient);
			}

			indexedStorageCapacity.gameObject.SetActive(true);
			indexedStorageCapacity.text = $"{occupiedSlots}/{indexedSlots.Count()}";

			closeStorageUIButton.transform.SetAsLastSibling();
			closeStorageUIButton.SetActive(true);
		}


		/// <summary>
		/// Called on slot update in any opened slot
		/// </summary>
		private void OnSlotContentsChangeClient()
		{
			int indexedSlotsCount = CurrentOpenStorage.ItemStorageStructure.IndexedSlots;
			int occupiedSlots = 0;
			foreach (var indexedSlot in CurrentOpenStorage.GetIndexedSlots())
			{
				if (indexedSlot.IsOccupied)
				{
					occupiedSlots++;
				}
			}

			indexedStorageCapacity.text = $"{occupiedSlots}/{indexedSlotsCount}";
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
			if (PlayerManager.LocalPlayerObject != null)
			{
				_ = SoundManager.PlayAtPosition(CommonSounds.Instance.Rustle,
					PlayerManager.LocalPlayerObject.transform.position,
					PlayerManager.LocalPlayerObject);
			}

			CurrentOpenStorage = null;
			foreach (var uiItemSlot in currentOpenStorageUISlots)
			{
				// remove listeners
				uiItemSlot.ItemSlot.OnSlotContentsChangeClient.RemoveListener(OnSlotContentsChangeClient);
				Destroy(uiItemSlot.transform.parent.gameObject);
			}

			currentOpenStorageUISlots.Clear();
			closeStorageUIButton.SetActive(false);
			indexedStorageCapacity.gameObject.SetActive(false);
		}
	}
}