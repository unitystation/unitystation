using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSwap : TooltipMonoBehaviour, IPointerClickHandler, IDropHandler,
	IPointerEnterHandler, IPointerExitHandler
{
	private UI_ItemSlot itemSlot;
	public override string Tooltip => itemSlot.hoverName;

	private Color32 successOverlayColor = new Color32(0, 255, 0, 92);
	private Color32 failOverlayColor = new Color32(255, 0, 0, 92);

	public void OnPointerClick(BaseEventData eventData)
	{
		OnPointerClick((PointerEventData)eventData);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			SoundManager.Play("Click01");
			// Only try interacting if we're not actually switching hands
			if (UIManager.Hands.hasSwitchedHands)
			{
				UIManager.Hands.hasSwitchedHands = false;
			}
			else
			{
				itemSlot.TryItemInteract();
			}
		}
	}

	private void Awake()
	{
		itemSlot = GetComponentInChildren<UI_ItemSlot>();
	}

	public new void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);

		var item = UIManager.Hands.CurrentSlot.Item;
		if (item == null
		    || itemSlot.Item != null
		    || itemSlot.namedSlot == NamedSlot.leftHand
		    || itemSlot.namedSlot == NamedSlot.rightHand)
		{
			return;
		}

		itemSlot.UpdateImage(item.gameObject,
			Validations.CanPutItemToSlot(PlayerManager.LocalPlayerScript, itemSlot.ItemSlot, item, NetworkSide.Client)
			? successOverlayColor : failOverlayColor);
	}

	public new void OnPointerExit(PointerEventData eventData)
	{
		base.OnPointerExit(eventData);

		itemSlot.UpdateImage(null);
	}

	//Means OnDrop while drag and dropping an Item. OnDrop is the UISlot that the mouse pointer is over when the user drops the item
	public void OnDrop(PointerEventData data)
	{
		if (UIManager.DragAndDrop.ItemSlotCache != null && UIManager.DragAndDrop.ItemCache != null)
		{
			var fromSlot = UIManager.DragAndDrop.ItemCache.GetComponent<Pickupable>().ItemSlot;

			//if there's an item storage in the slot, request to put the item in the storage
			var destStorage = itemSlot.ItemSlot.Item?.GetComponent<InteractableStorage>();
			if (destStorage != null)
			{
				//rather than try to figure out which indexed slot it should go in,
				//just perform the normal inventory apply interaction with this slot
				InteractionUtils.RequestInteract(InventoryApply.ByLocalPlayer(itemSlot.ItemSlot), destStorage);
			}
			else
			{
				Inventory.ClientRequestTransfer(fromSlot, itemSlot.ItemSlot);
			}
		}
	}
}