using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSwap : MonoBehaviour, IPointerClickHandler, IDropHandler
{
	private UI_ItemSlot itemSlot;

	public void OnPointerClick(BaseEventData eventData)
	{
		OnPointerClick((PointerEventData)eventData);
	}
	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			SoundManager.Play("Click01");
			UIManager.Hands.SwapItem(itemSlot);
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
			if (itemSlot.Item == null && itemSlot.CheckItemFit(UIManager.DragAndDrop.ItemCache))
			{
				if (PlayerManager.LocalPlayerScript != null)
				{
					if (!PlayerManager.LocalPlayerScript.playerMove.allowInput ||
						PlayerManager.LocalPlayerScript.playerMove.isGhost)
					{
						return;
					}
				}

				UIManager.TryUpdateSlot(new UISlotObject(itemSlot.inventorySlot.UUID, UIManager.DragAndDrop.ItemCache,
					UIManager.DragAndDrop.ItemSlotCache?.inventorySlot.UUID));
			}
			// else if (itemSlot.Item != null)
			// {
			// 	//Check if it is a storage obj:
			// 	var storageObj = itemSlot.Item.GetComponent<StorageObject>();
			// 	if (storageObj != null)
			// 	{
			// 		if(storageObj.NextSpareSlot() != null){
			// 			UIManager.TryUpdateSlot(new UISlotObject(storageObj.NextSpareSlot().UUID, UIManager.DragAndDrop.ItemCache,
			// 		UIManager.DragAndDrop.ItemSlotCache?.inventorySlot.UUID));
			// 		}
			// 	}
			// }
		}
	}
}