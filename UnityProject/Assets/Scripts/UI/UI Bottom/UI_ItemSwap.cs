using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSwap : TooltipMonoBehaviour, IPointerClickHandler, IDropHandler
{
	private UI_ItemSlot itemSlot;
	public override string Tooltip => itemSlot.hoverName;

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

	private void Start()
	{
		itemSlot = GetComponentInChildren<UI_ItemSlot>();
	}

	//Means OnDrop while drag and dropping an Item. OnDrop is the UISlot that the mouse pointer is over when the user drops the item
	public void OnDrop(PointerEventData data)
	{
		if (UIManager.DragAndDrop.ItemSlotCache != null && UIManager.DragAndDrop.ItemCache != null)
		{
			if (itemSlot.inventorySlot.IsUISlot)
			{
				if(itemSlot.Item == null)
				{
					if (itemSlot.CheckItemFit(UIManager.DragAndDrop.ItemCache))
					{
						if (PlayerManager.LocalPlayerScript != null)
						{
							if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
								PlayerManager.LocalPlayerScript.IsGhost)
							{
								return;
							}
						}
						if(UIManager.DragAndDrop.ItemSlotCache.inventorySlot.IsUISlot)
						{
							PlayerManager.LocalPlayerScript.playerNetworkActions.CmdUpdateSlot(itemSlot.equipSlot, UIManager.DragAndDrop.ItemSlotCache.equipSlot);
						}
						else
						{
							var storage = UIManager.DragAndDrop.ItemSlotCache.inventorySlot;
							StoreItemMessage.Send(storage.Owner, PlayerManager.LocalPlayerScript.gameObject, itemSlot.equipSlot, false, storage.equipSlot);
						}

					}
				}
				else
				{
					var storage = itemSlot.Item.GetComponent<InteractableStorage>();
					if (storage)
					{
						storage.StoreItem(PlayerManager.LocalPlayerScript.gameObject, UIManager.DragAndDrop.ItemSlotCache.equipSlot, UIManager.DragAndDrop.ItemCache);
					}
				}
			}
			else
			{
				var storage = itemSlot.inventorySlot.Owner.GetComponent<InteractableStorage>();
				storage.StoreItem(PlayerManager.LocalPlayerScript.gameObject, UIManager.DragAndDrop.ItemSlotCache.equipSlot, UIManager.DragAndDrop.ItemCache);
			}
		}
	}
}