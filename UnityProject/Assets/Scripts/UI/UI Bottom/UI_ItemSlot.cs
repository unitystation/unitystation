using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UI_ItemSlot : MonoBehaviour, IDragHandler, IEndDragHandler
{
	public bool allowAllItems;
	public List<ItemType> allowedItemTypes;
	public string eventName;

	[HideInInspector]
	public Image image;

	private Image secondaryImage; //For sprites that require two images
	public ItemSize maxItemSize;

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

	public bool IsFull => Item != null;

	//Inventoryslot theifing is prevented by the UUID system 
	//(clients don't know what other clients UUID's are and all slots are server authorative with validation checks)
	public InventorySlot inventorySlot { get; set; }

	private void Awake()
	{
		image = GetComponent<Image>();
		inventorySlot = new InventorySlot(System.Guid.Empty, eventName, true);
		secondaryImage = GetComponentsInChildren<Image>()[1];
		secondaryImage.alphaHitTestMinimumThreshold = 0.5f;
		secondaryImage.enabled = false;
		image.alphaHitTestMinimumThreshold = 0.5f;
		image.enabled = false;
		if (eventName.Length > 0)
		{
			//				Logger.LogTraceFormat("Triggered SetItem for {0}", Category.UI, eventName);
			EventManager.UI.AddListener(eventName, SetItem);
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
		StartCoroutine(SetSlotOnEnable());
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

	IEnumerator SetSlotOnEnable()
	{
		yield return YieldHelper.EndOfFrame;
		if (!InventoryManager.AllClientInventorySlots.Contains(inventorySlot))
		{
			InventoryManager.AllClientInventorySlots.Add(inventorySlot);
		}
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
		image.enabled = true;
		image.preserveAspect = true;
		Item = item;
		item.transform.position = TransformState.HiddenPos;
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
		if (!lps || lps.canNotInteract())
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

	/// <summary>
	///     Clientside check for dropping/placing objects from inventory slot
	/// </summary>
	public bool CanPlaceItem()
	{
		return IsFull && UIManager.SendUpdateAllowed(Item);
	}

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
			if (!allowedItemTypes.Contains(attributes.type))
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
		if (allowAllItems || allowedItemTypes.Contains(attributes.type))
		{
			allowed = true;
		}
		if (!inventorySlot.IsUISlot && UIManager.StorageHandler.storageCache?.gameObject == item)
		{
			allowed = false;
		}
		return allowed;
	}

	public void TryItemInteract()
	{
		if (eventName != "leftHand" && eventName != "rightHand")
		{
			//Clicked on item in another slot other then hands
			if (Item != null)
			{
				var inputTrigger = Item.GetComponent<InputTrigger>();
				inputTrigger.UI_InteractOtherSlot(PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.Item);
				return;
			}
		}

		if (Item != null && UIManager.Hands.CurrentSlot.eventName == eventName)
		{
			var inputTrigger = Item.GetComponent<InputTrigger>();
			inputTrigger.UI_Interact(PlayerManager.LocalPlayer, eventName);
		}
		else
		{
			if (UIManager.Hands.CurrentSlot.eventName != eventName)
			{
				//Clicked on item with otherslot selected
				if (UIManager.Hands.OtherSlot.Item != null)
				{
					var trig = UIManager.Hands.OtherSlot.Item.GetComponent<InputTrigger>();
					if (trig != null)
					{
						trig.UI_InteractOtherSlot(PlayerManager.LocalPlayer,
							UIManager.Hands.CurrentSlot.Item);
					}
				}
			}
		}
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