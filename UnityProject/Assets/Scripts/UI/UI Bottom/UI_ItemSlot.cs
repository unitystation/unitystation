using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_ItemSlot : TooltipMonoBehaviour, IDragHandler, IEndDragHandler
{
	public bool allowAllItems;
	public List<ItemType> allowedItemTypes;
	public string eventName;
	public string hoverName;
	public EquipSlot equipSlot;
	[HideInInspector]
	public InventorySlot inventorySlot;

	[HideInInspector]
	public Image image;

	private Image secondaryImage; //For sprites that require two images
	public ItemSize maxItemSize;

	/// pointer is over the actual item in the slot due to raycast target
	public override string Tooltip => Item.GetComponent<ItemAttributes>().itemName;

	/// set back to the slot name since the pointer is still over the slot background
	public override string ExitTooltip => hoverName;

	public GameObject Item
	{
		get
		{
			return inventorySlot.Item;
		}
		set
		{
			inventorySlot.Item = value;
		}
	}

	private void Awake() {
		inventorySlot = new InventorySlot(equipSlot, true, gameObject);
		image = GetComponent<Image>();
		secondaryImage = GetComponentsInChildren<Image>()[1];
		secondaryImage.alphaHitTestMinimumThreshold = 0.5f;
		secondaryImage.enabled = false;
		image.alphaHitTestMinimumThreshold = 0.5f;
		image.enabled = false;
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
	///     direct low-level method, doesn't send anything to server
	/// </summary>
	public void SetItem(GameObject item)
	{
		if (!item)
		{
			Clear();
			return;
		}
		Logger.LogTraceFormat("Setting item {0} to {1}", Category.UI, item.name, eventName);

		UpdateImage(item);

		image.enabled = true;
		image.preserveAspect = true;
		Item = item;
		item.transform.position = TransformState.HiddenPos;
	}

	public void UpdateImage(GameObject item)
	{
		var spriteRends = item.GetComponentsInChildren<SpriteRenderer>();
		if (image == null)
		{
			image = GetComponent<Image>();
		}
		image.sprite = spriteRends[0].sprite;
		if (spriteRends.Length > 1)
		{
			if (spriteRends[1].sprite != null)
			{
				SetSecondaryImage(spriteRends[1].sprite);
			}
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

	//        public bool TrySetItem(GameObject item) {
	//            if(!IsFull && item != null && CheckItemFit(item)) {
	////                Debug.LogErrorFormat("TrySetItem TRUE for {0}", item.GetComponent<ItemAttributes>().hierarchy);
	//                InventoryInteractMessage.Send(eventName, item, true);
	//               //predictions:
	//                UIManager.UpdateSlot(new UISlotObject(eventName, item));
	////                SetItem(item);
	//
	//                return true;
	//            }
	////            Debug.LogErrorFormat("TrySetItem FALSE for {0}", item.GetComponent<ItemAttributes>().hierarchy);
	//            return false;
	//        }

	/// <summary>
	///     removes item from slot
	/// </summary>
	/// <returns></returns>
	public GameObject Clear()
	{
		PlayerScript lps = PlayerManager.LocalPlayerScript;
		if (!lps)
		{
			return null;
		}

		GameObject item = Item;
		//            InputTrigger.Touch(Item);
		Item = null;
		image.enabled = false;
		secondaryImage.enabled = false;
		ControlTabs.CheckTabClose();
		image.sprite = null;
		secondaryImage.sprite = null;
		return item;
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
	public bool PlaceItem(Vector3 pos)
	{
		var item = Clear();
		if (!item)
		{
			return false;
		}
		var itemTransform = item.GetComponent<CustomNetTransform>();
		itemTransform.AppearAtPosition(pos);
		var itemAttributes = item.GetComponent<ItemAttributes>();
		Logger.LogTraceFormat("Placing item {0}/{1} from {2} to {3}", Category.UI, item.name, itemAttributes ? itemAttributes.itemName : "(no iAttr)", eventName, pos);
		ControlTabs.CheckTabClose();
		return true;
	}

	public void Reset()
	{
		image.sprite = null;
		image.enabled = false;
		secondaryImage.sprite = null;
		secondaryImage.enabled = false;
		Item = null;
		ControlTabs.CheckTabClose();
	}

	public bool CheckItemFit(GameObject item)
	{
		ItemAttributes attributes = item.GetComponent<ItemAttributes>();

		if (!allowAllItems)
		{
			if (!allowedItemTypes.Contains(attributes.itemType))
			{
				return false;
			}
		}
		else if (attributes.size > maxItemSize)
		{
			Logger.LogWarning($"{attributes.size} {item} is too big for {maxItemSize} {eventName}!", Category.UI);
			return false;
		}

		bool allowed = false;
		if (allowAllItems || allowedItemTypes.Contains(attributes.itemType))
		{
			allowed = true;
		}
		if (!inventorySlot.IsUISlot && UIManager.StorageHandler.storageCache?.gameObject == item)
		{
			allowed = false;
		}
		return allowed;
	}


	/// <summary>
	/// Check if item has an interaction with a an item in a slot
	/// If not or if bool returned is true, swap items
	/// </summary>
	public void TryItemInteract()
	{
		// Clicked on another slot other than hands
		if (equipSlot != EquipSlot.leftHand && equipSlot != EquipSlot.rightHand)
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
		if (Item != null && UIManager.Hands.CurrentSlot.equipSlot == equipSlot)
		{
			//check IF2 logic first
			var interactables = Item.GetComponents<IInteractable<HandActivate>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			var activate = HandActivate.ByLocalPlayer();
			foreach (var interactable in interactables)
			{
				if (interactable.Interact(activate))
				{
					return;
				}
			}
		}
		else
		{
			if (UIManager.Hands.CurrentSlot.equipSlot != equipSlot)
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
			var combine = InventoryApply.ByLocalPlayer(inventorySlot);
			//check interactables in the active hand (if active hand occupied)
			if (UIManager.Hands.CurrentSlot.Item != null)
			{
				var handInteractables = UIManager.Hands.CurrentSlot.Item.GetComponents<IInteractable<InventoryApply>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				foreach (var interactable in handInteractables)
				{
					if (interactable.Interact(combine))
					{
						//something combined, don't do anything else
						return true;
					}
				}
			}

			//check interactables in the target
			var targetInteractables = Item.GetComponents<IInteractable<InventoryApply>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			foreach (var interactable in targetInteractables)
			{
				if (interactable.Interact(combine))
				{
					//something combined, don't do anything else
					return true;
				}
			}


		}

		return false;
	}

	public void OnDrag(PointerEventData data)
	{
		UIManager.DragAndDrop.UI_ItemDrag(this);
	}

	public void OnEndDrag(PointerEventData data)
	{
		UIManager.DragAndDrop.StopDrag();
	}
}