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
		Debug.Log("ON POINTER CLICK");
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
		Debug.Log("DROPPED: " + UIManager.DragAndDrop.ItemCache.name);
	}
}