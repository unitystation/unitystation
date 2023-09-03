using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using HealthV2;
using Items.Implants.Organs;
using Logs;
using Managers;
using UI;

/// <summary>
/// Represents an item slot rendered in the UI.
/// </summary>
[Serializable]
public class UI_ItemSlot : TooltipMonoBehaviour
{
	[SerializeField]
	[FormerlySerializedAs("NamedSlot")]
	[Tooltip("For player inventory, named slot in player's ItemStorage that this UI slot corresponds to.")]
	protected NamedSlot namedSlot = NamedSlot.back;

	public NamedSlot NamedSlot => namedSlot;

	[Tooltip("whether this is for the local player's top level inventory or will be instead used" +
	         " for another player's inventory.")]
	[SerializeField]
	protected bool forLocalPlayer = false;

	[Tooltip("Name to display when hovering over this slot in the UI")] [SerializeField]
	protected string hoverName = null;

	[Tooltip("Whether this slot is initially visible in the UI.")] [SerializeField]
	protected bool initiallyHidden = false;

	[Tooltip("Placeholder image that will be disabled when there is an item in slot")] [SerializeField]
	protected Image placeholderImage = null;

	[Tooltip("From where the item slot is linked from")]
	public ItemStorageLinkOrigin ItemStorageLinkOrigin = ItemStorageLinkOrigin.localPlayer;

	/// pointer is over the actual item in the slot due to raycast target. If item ghost, return slot tooltip
	public override string Tooltip => Item == null ? ExitTooltip : Item.GetComponent<ItemAttributesV2>().ArticleName;

	/// set back to the slot name since the pointer is still over the slot background
	public override string ExitTooltip => hoverName;

	/// <summary>
	/// Item in this slot, null if empty.
	/// </summary>
	public Pickupable Item => itemSlot?.Item;

	/// <summary>
	/// Actual slot this UI slot is linked to
	/// </summary>
	public ItemSlot ItemSlot => itemSlot;

	/// <summary>
	/// GameObject of the item equipped in this slot, null if not equipped.
	/// (Convenience method for not having to do Item.gameObject)
	/// </summary>
	public GameObject ItemObject => itemSlot.ItemObject;

	public UI_ItemImage Image => image;

	private bool hidden;
	private UI_ItemImage image;
	private ItemSlot itemSlot;
	public Text amountText;

	public Image MoreInventoryImage;
	public HasSubInventory HasSubInventory;

	public Material OverlayMaterial;

	private void Awake()
	{
		if (amountText)
		{
			amountText.enabled = false;
		}

		if (MoreInventoryImage)
		{
			MoreInventoryImage.enabled = false;
		}

		image = new UI_ItemImage(gameObject, OverlayMaterial);
		hidden = initiallyHidden;
	}

	/// <summary>
	/// Link this item slot to its configured named slot on the local player, if this slot is for the local player.
	/// Should only be called after local player is spawned.
	/// </summary>
	public void LinkToLocalPlayer()
	{
		if (namedSlot != NamedSlot.none && forLocalPlayer)
		{
			var linkedSlot = ItemSlot.GetNamed(GetItemStorage(), namedSlot);
			if (linkedSlot != null)
			{
				LinkSlot(linkedSlot);
			}
		}
	}

	private ItemStorage GetItemStorage()
	{
		if (ItemStorageLinkOrigin == ItemStorageLinkOrigin.localPlayer)
		{
			return null;
		}
		else
		{
			return AdminManager.Instance.LocalAdminGhostStorage;
		}
	}

	/// <summary>
	/// Link this item slot to display the contents of the indicated slot, updating whenever the contents change.
	/// </summary>
	/// <param name="linkedSlot"></param>
	public void LinkSlot(ItemSlot linkedSlot)
	{
		if (itemSlot != null)
		{
			//stop observing this slot
			itemSlot.LinkLocalUISlot(null);
			itemSlot.OnSlotContentsChangeClient.RemoveListener(OnClientSlotContentsChange);
		}

		//start observing the new slot
		itemSlot = linkedSlot;
		if (itemSlot != null)
		{
			itemSlot.LinkLocalUISlot(this);
			itemSlot.OnSlotContentsChangeClient.AddListener(OnClientSlotContentsChange);
		}

		RefreshImage();
	}


	/// <summary>
	///  any relation to any slot on client
	/// </summary>
	/// <param name="linkedSlot"></param>
	public void UnLinkSlot()
	{
		if (itemSlot != null)
		{
			//stop observing this slot
			itemSlot.LinkLocalUISlot(null);
			itemSlot.OnSlotContentsChangeClient.RemoveListener(OnClientSlotContentsChange);
			itemSlot = null;
		}
	}

	public void SetUp(BodyPartUISlots.StorageCharacteristics storageCharacteristics)
	{
		if (placeholderImage != null)placeholderImage.sprite = storageCharacteristics.placeholderSprite;
		namedSlot = storageCharacteristics.namedSlot;
		hoverName = storageCharacteristics.hoverName;
	}

	private void OnClientSlotContentsChange()
	{
		//callback for when our item slot's contents change.
		//We update our sprite
		var item = itemSlot.Item;
		if (!item)
		{
			Clear();
			return;
		}

		RefreshImage();
	}

	/// <summary>
	/// Update the image displayed in the slot based on the slots current contents
	/// </summary>
	public void RefreshImage()
	{
		if (itemSlot != null)
			UpdateImage(ItemObject);
	}

	/// <summary>
	/// Update the image that should be displayed in this slot to display the sprite of the specified item.
	///
	/// If hidden, effect will not be visible until this slot is unhidden
	///
	/// </summary>
	/// <param name="item">game object to use to determine what to show in this slot</param>
	/// <param name="color">color tint to apply</param>
	public void UpdateImage(GameObject item = null, Color? color = null)
	{
		bool nullItem = item == null;
		bool forceColor = color != null;

		if (nullItem && Item != null)
		{
			// Case for when we have a hovered image and insert, then stop hovering
			return;
		}

		// If player is cuffed, a special icon appears on his hand slots, exit without changing it.
		if ((namedSlot == NamedSlot.leftHand || namedSlot == NamedSlot.rightHand) &&
		    PlayerManager.LocalPlayerScript.playerMove.IsCuffed)
		{
			return;
		}

		if (!nullItem)
		{
			image?.ShowItem(item,OverlayMaterial,  color);
			if (placeholderImage)
				placeholderImage.color = new Color(1, 1, 1, 0);

			//determine if we should show an amount
			var stack = item.GetComponent<Stackable>();
			if (stack != null && ((stack.Amount > 1  && amountText)|| stack.IsRepresentationOfStack) )
			{
				amountText.enabled = true;
				amountText.text = stack.Amount.ToString();
			}
			else if (stack != null && stack.Amount <= 1 && amountText)
			{
				//remove the stack display
				amountText.enabled = false;
			}

			if (MoreInventoryImage != null)
			{
				var Storage = item.GetComponent<InteractableStorage>();
				if (Storage != null && Storage.DoNotShowInventoryOnUI == false)
				{
					HasSubInventory.itemStorage = Storage.ItemStorage;
					MoreInventoryImage.enabled = true;
				}
				else
				{
					HasSubInventory.itemStorage = null;
					MoreInventoryImage.enabled = false;
				}
			}
		}
		else
		{
			//no object was passed, so clear out the sprites
			Clear();
		}
	}

	public void SetSecondaryImage(Sprite sprite)
	{
		image.SetOverlay(sprite);
	}

	/// <summary>
	/// Clears the displayed image.
	/// </summary>
	public void Clear()
	{
		PlayerScript lps = PlayerManager.LocalPlayerScript;
		if (!lps)
		{
			return;
		}

		image?.ClearAll();
		if (amountText)
		{
			amountText.enabled = false;
		}

		if (placeholderImage)
		{
			placeholderImage.color = Color.white;
		}

		if (HasSubInventory)
		{
			HasSubInventory.itemStorage = null;
		}

		if (MoreInventoryImage)
		{
			MoreInventoryImage.enabled = false;
		}
	}

	public void Reset()
	{
		image.ClearAll();
		if (amountText)
		{
			amountText.enabled = false;
		}

		if (placeholderImage)
		{
			placeholderImage.color = Color.white;
		}

		if (MoreInventoryImage)
		{
			HasSubInventory.itemStorage = null;
			MoreInventoryImage.enabled = false;
		}

		ControlTabs.CheckTabClose();
	}

	private bool isValidPlayer()
	{
		if (PlayerManager.LocalPlayerScript == null) return false;

		// TODO tidy up this if statement once it's working correctly
		if (!PlayerManager.LocalPlayerScript.playerMove.AllowInput ||
		    PlayerManager.LocalPlayerScript.IsGhost)
		{
			Loggy.Log("Invalid player, cannot perform action!", Category.Interaction);
			return false;
		}

		return true;
	}

	public bool SwapItem(UI_ItemSlot itemSlot)
	{
		if (isValidPlayer())
		{
			var CurrentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if (CurrentSlot != itemSlot.itemSlot) //Check if we're not interacting with our own hand
			{
				if (CurrentSlot.Item == null) //check if hand is empty
				{
					if (itemSlot.Item != null) //check if slot is not empty
					{
						//if slot is not empty and hand is empty; ask the inventory to give us that item in our hand
						Inventory.ClientRequestTransfer(itemSlot.ItemSlot, CurrentSlot);
						return true;
					}
				}
				else
				{
					if (itemSlot.Item != null) return false;
					//if slot is empty, ask the game to put whatever thats in out hand in it.
					Inventory.ClientRequestTransfer(CurrentSlot, itemSlot.ItemSlot);
					return true;
				}
			}
		}
		return false;
	}

	/// <summary>
	/// Check if item has an interaction with a an item in a slot
	/// If not or if bool returned is true, swap items
	/// </summary>
	public void TryItemInteract(bool swapIfEmpty = true)
	{
		// Clicked on another slot other than our own hands
		bool IsHandSlots = false;
		IsHandSlots = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot() == itemSlot;

		if (IsHandSlots == false)
		{
			// If full, attempt to interact the two, otherwise swap
			if (Item != null)
			{
				//check IF2 InventoryApply interaction - combine the active hand item with this (only if
				//both are occupied)
				if (TryIF2InventoryApply()) return;

				if (swapIfEmpty)
					SwapItem(this);
				return;
			}
			else
			{
				if (swapIfEmpty)
					SwapItem(this);
				return;
			}
		}

		// If there is an item and the hand is interacting in the same slot
		if (Item != null && PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot() == itemSlot)
		{
			//check IF2 logic first
			var interactables = Item.GetComponents<IBaseInteractable<HandActivate>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			var activate = HandActivate.ByLocalPlayer();
			InteractionUtils.ClientCheckAndTrigger(interactables, activate);
		}
		else
		{
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot() != itemSlot)
			{
				if (TryIF2InventoryApply()) return;
				if (swapIfEmpty)
					SwapItem(this);
			}
		}
	}


	private bool TryIF2InventoryApply()
	{
		//check IF2 InventoryApply interaction - apply the active hand item with this (only if
		//target slot is occupied, but it's okay if active hand slot is not occupied)
		if (Item != null)
		{
			var combine = InventoryApply.ByLocalPlayer(itemSlot, PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot());
			//check interactables in the active hand (if active hand occupied)
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item != null)
			{
				if (combine.IsAltClick && SwapTwoItemsInInventory(combine.FromSlot)) return true;
				var handInteractables = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item
					.GetComponents<IBaseInteractable<InventoryApply>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(handInteractables, combine) != null) return true;
			}

			//check interactables in the target
			var targetInteractables = Item.GetComponents<IBaseInteractable<InventoryApply>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			if (InteractionUtils.ClientCheckAndTrigger(targetInteractables, combine) != null) return true;
		}

		return false;
	}

	private bool SwapTwoItemsInInventory(ItemSlot CurrentSlot)
    	{
    		if (PlayerManager.LocalPlayerScript.PlayerNetworkActions == null) return false;
            PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdServerReplaceItemInInventory(CurrentSlot.ItemObject,
    			itemSlot.ItemStorageNetID, itemSlot.NamedSlot.Value);
            return true;
    	}


	[ContextMenu("Debug Slot")]
	void DebugItem()
	{
		Loggy.Log(itemSlot.ToString(), Category.PlayerInventory);
	}

	/// <summary>
	/// Sets whether this should be shown / hidden (but the set sprites will still be remembered when it is unhidden)
	/// </summary>
	/// <param name="hidden"></param>
	public void SetHidden(bool hidden)
	{
		this.hidden = hidden;
		image.SetHidden(hidden);
		if (hidden && amountText)
		{
			amountText.enabled = false;
		}
		else if (!hidden)
		{
			//show if we have something stackable.
			if (itemSlot?.ItemObject != null)
			{
				if (amountText)
				{
					var stack = itemSlot.ItemObject.GetComponent<Stackable>();
					if (stack != null && stack.Amount > 1)
					{
						amountText.enabled = true;
					}
				}

				if (MoreInventoryImage != null)
				{
					var Storage = itemSlot.ItemObject.GetComponent<InteractableStorage>();
					if (Storage != null)
					{
						HasSubInventory.itemStorage = Storage.ItemStorage;
						MoreInventoryImage.enabled = true;
					}
					else
					{
						HasSubInventory.itemStorage = null;
						MoreInventoryImage.enabled = false;
					}
				}
			}

			if (Item && placeholderImage)
			{
				placeholderImage.color = new Color(1, 1, 1, 0);
			}
		}
	}
}

public enum ItemStorageLinkOrigin
{
	localPlayer = 0,
	adminGhost = 1,
}
