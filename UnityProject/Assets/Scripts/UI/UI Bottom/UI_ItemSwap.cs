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

				UIManager.TryUpdateSlot(new UISlotObject(itemSlot.eventName, UIManager.DragAndDrop.ItemCache));
			}
		}
	}
}