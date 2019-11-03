using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Represents an item slot rendered in the UI.
/// </summary>
public class UI_ItemSlot : TooltipMonoBehaviour, IDragHandler, IEndDragHandler
{
	//TODO: Remove after I've copied these to the new assets.
	public bool allowAllItems;
	public List<ItemType> allowedItemTypes;
	public string hoverName;
	[Tooltip("For player inventory, named slot in local player's ItemStorage that this UI slot corresponds to.")]
	public NamedSlot? namedSlot;

	[HideInInspector]
	public Image image;

	private Image secondaryImage; //For sprites that require two images
	public ItemSize maxItemSize;

	/// pointer is over the actual item in the slot due to raycast target. If item ghost, return slot tooltip
	public override string Tooltip => Item == null ? ExitTooltip : Item.GetComponent<ItemAttributes>().itemName;

	/// set back to the slot name since the pointer is still over the slot background
	public override string ExitTooltip => hoverName;

	/// <summary>
	/// Item in this slot, null if empty.
	/// </summary>
	public Pickupable Item => itemSlot.Item;

	/// <summary>
	/// Actual slot this UI slot is linked to
	/// </summary>
	public ItemSlot ItemSlot => itemSlot;

	/// <summary>
	/// GameObject of the item equipped in this slot, null if not equipped.
	/// (Convenience method for not having to do Item.gameObject)
	/// </summary>
	public GameObject ItemObject => itemSlot.ItemObject;

	private ItemSlot itemSlot;

	private void Awake() {

		image = GetComponent<Image>();
		secondaryImage = GetComponentsInChildren<Image>()[1];
		secondaryImage.alphaHitTestMinimumThreshold = 0.5f;
		secondaryImage.enabled = false;
		image.alphaHitTestMinimumThreshold = 0.5f;
		image.enabled = false;
		if (namedSlot != null)
		{
			LinkSlot(ItemSlot.GetNamed(PlayerManager.LocalPlayerScript.ItemStorage, (NamedSlot) namedSlot));
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	//Reset Item slot sprite on game restart
	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		image.sprite = null;
		image.enabled = false;
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

		UpdateImage();
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

		UpdateImage(item.gameObject);

		//TODO: This shouldn't be needed, right?
		//item.transform.position = TransformState.HiddenPos;
	}

	//TODO: Refactor the below to be done using a callback / interface / hook type of system, probably create a component which manages the UI appearance

	/// <summary>
	/// Update the image displayed in this slot based on what is currently in the slot, optionally overriding
	/// the slot to show the appearance of a different item or change the color.
	///
	/// For example, you can call this if the equipped item's sprite has changed and you want to reflect that change
	/// in the UI.
	/// </summary>
	/// <param name="overrideItem">game object to use instead of the item that is currently in this slot in order
	/// to derive the image to show in this slot</param>
	/// <param name="color">color tint to apply</param>
	public void UpdateImage(GameObject overrideItem = null, Color? color = null)
	{
		var useColor = color.GetValueOrDefault(Color.white);
		var forceColor = color != Color.white;

		var nullItem = overrideItem == null;
		var itemToRender = overrideItem;
		if (itemToRender == null && Item != null)
		{
			itemToRender = Item.gameObject;
		}

		if (itemToRender == null)
		{
			Clear();
			return;
		}

		var spriteRends = itemToRender.GetComponentsInChildren<SpriteRenderer>();
		if (image == null)
		{
			image = GetComponent<Image>();
		}
		image.sprite = spriteRends[0].sprite;
		image.color = spriteRends[0].color;
		if (spriteRends.Length > 1)
		{
			if (spriteRends[1].sprite != null)
			{
				SetSecondaryImage(spriteRends[1].sprite);
				secondaryImage.color = spriteRends[1].color;
			}
		}

		else
		{
			Clear();
		}

		if (forceColor)
		{
			image.color = useColor;
		}

		image.enabled = !nullItem;
		image.preserveAspect = !nullItem;

		if (secondaryImage)
		{
			if (forceColor)
			{
				secondaryImage.color = useColor;
			}

			secondaryImage.enabled = secondaryImage.sprite != null && !nullItem;
			secondaryImage.preserveAspect = !nullItem;
		}
	}

	public void SetSecondaryImage(Sprite sprite)
	{
		if (sprite != null)
		{
			secondaryImage.sprite = sprite;
			secondaryImage.enabled = true;
			secondaryImage.preserveAspect = true;
		}
		else
		{
			secondaryImage.sprite = null;
			secondaryImage.enabled = false;
		}
	}

	/// <summary>
	///     removes item from slot
	/// </summary>
	/// <returns></returns>
	private void Clear()
	{
		PlayerScript lps = PlayerManager.LocalPlayerScript;
		if (!lps)
		{
			return;
		}

		image.enabled = false;
		secondaryImage.enabled = false;
		ControlTabs.CheckTabClose();
		image.sprite = null;
		secondaryImage.sprite = null;
	}

/*
	/// <summary>
	///     Clientside check for dropping/placing objects from inventory slot
	/// </summary>
	public bool CanPlaceItem()
	{
		return IsFull && UIManager.SendUpdateAllowed(Item);
	}
 */
	/// <summary>
	///     clientside simulation of placement
	/// </summary>
	/// TODO: Is this still needed?
//	public bool PlaceItem(Vector3 pos)
//	{
//		var item = Clear();
//		if (!item)
//		{
//			return false;
//		}
//		var itemTransform = item.GetComponent<CustomNetTransform>();
//		itemTransform.AppearAtPosition(pos);
//		var itemAttributes = item.GetComponent<ItemAttributes>();
//		Logger.LogTraceFormat("Placing item {0}/{1} from {2} to {3}", Category.UI, item.name, itemAttributes ? itemAttributes.itemName : "(no iAttr)", eventName, pos);
//		ControlTabs.CheckTabClose();
//		return true;
//	}

	public void Reset()
	{
		image.sprite = null;
		image.enabled = false;
		secondaryImage.sprite = null;
		secondaryImage.enabled = false;
		ControlTabs.CheckTabClose();
	}

	public bool CheckItemFit(GameObject item)
	{
		var pickupable = item.GetComponent<Pickupable>();
		if (pickupable == null) return false;
		return itemSlot.CanFit(pickupable);
	}


	/// <summary>
	/// Check if item has an interaction with a an item in a slot
	/// If not or if bool returned is true, swap items
	/// </summary>
	public void TryItemInteract()
	{

		var slotName = itemSlot.SlotIdentifier.NamedSlot;
		// Clicked on another slot other than hands
		if (slotName != NamedSlot.leftHand && slotName != NamedSlot.rightHand)
		{
			// If full, attempt to interact the two, otherwise swap
			if (Item != null)
			{
				//check IF2 InventoryApply interaction - combine the active hand item with this (only if
				//both are occupied)
				if (TryIF2InventoryApply()) return;

				UIManager.Hands.SwapItem(this);
				return;
			}
			else
			{
				UIManager.Hands.SwapItem(this);
				return;
			}
		}
		// If there is an item and the hand is interacting in the same slot
		if (Item != null && UIManager.Hands.CurrentSlot.ItemSlot == itemSlot)
		{
			//check IF2 logic first
			var interactables = Item.GetComponents<IBaseInteractable<HandActivate>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			var activate = HandActivate.ByLocalPlayer();
			InteractionUtils.ClientCheckAndTrigger(interactables, activate);
		}
		else
		{
			if (UIManager.Hands.CurrentSlot.ItemSlot != itemSlot)
			{
				//Clicked on item with otherslot selected
				if (UIManager.Hands.OtherSlot.Item != null)
				{
					if (TryIF2InventoryApply()) return;
					UIManager.Hands.SwapItem(this);
				}
			}
		}
	}

	private bool TryIF2InventoryApply()
	{
		//check IF2 InventoryApply interaction - apply the active hand item with this (only if
		//target slot is occupied, but it's okay if active hand slot is not occupied)
		if (Item != null)
		{
			var combine = InventoryApply.ByLocalPlayer(itemSlot);
			//check interactables in the active hand (if active hand occupied)
			if (UIManager.Hands.CurrentSlot.Item != null)
			{
				var handInteractables = UIManager.Hands.CurrentSlot.Item.GetComponents<IBaseInteractable<InventoryApply>>()
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

	public void OnDrag(PointerEventData data)
	{
		if (data.button == PointerEventData.InputButton.Left)
		{
			UIManager.DragAndDrop.UI_ItemDrag(this);
		}
	}

	public void OnEndDrag(PointerEventData data)
	{
		UIManager.DragAndDrop.StopDrag();
	}


	[ContextMenu("Debug Slot")]
	void DebugItem()
	{
		Logger.Log(itemSlot.ToString(), Category.Inventory);
	}
}