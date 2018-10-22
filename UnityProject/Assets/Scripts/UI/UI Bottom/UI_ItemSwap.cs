using UnityEngine;
using UnityEngine.EventSystems;

public class UI_ItemSwap : MonoBehaviour, IPointerClickHandler
{
	private UI_ItemSlot itemSlot;

	public void OnDragStart()
	{
		UIManager.DragAndDrop.StartDrag(itemSlot.Item);
	}

	public void OnDragEnd()
	{
		Debug.Log("END DRAG");
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Left)
		{
			SoundManager.Play("Click01");
			if (itemSlot.eventName == "back")
			{
				//Backpacks and belts with storage need to be dragged and dropped to 
				//return back to hand. Instead clicking on them with an empty hand will open them.
				//Clicking on them with an item in the hand will put the item in the bag

				if (itemSlot.Item != null)
				{
					itemSlot.Item.GetComponent<InputTrigger>()?.UI_Interact(PlayerManager.LocalPlayer, UIManager.Hands.CurrentSlot.eventName);
				}

			}
			else
			{
				UIManager.Hands.SwapItem(itemSlot);
			}
		}
	}

	private void Start()
	{
		itemSlot = GetComponentInChildren<UI_ItemSlot>();
	}
}