using System;
using System.Linq;
using Logs;
using Messages.Client.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;

namespace UI
{
	public class UI_ItemSwap : TooltipMonoBehaviour, IPointerClickHandler, IDropHandler,
		IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
	{
		private UI_ItemSlot itemSlot;
		public override string Tooltip => itemSlot.NamedSlot.ToString();

		private Color32 successOverlayColor = new Color32(0, 255, 0, 92);
		private Color32 failOverlayColor = new Color32(255, 0, 0, 92);

		public void OnPointerClick(BaseEventData eventData)
		{
			OnPointerClick((PointerEventData)eventData);
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button == PointerEventData.InputButton.Left && !eventData.dragging)
			{
				OnClick();
			}
		}

		public void OnClick()
		{
			// If shift is pressed, don't check anything, just send Examine on contained item if any.
			if (KeyboardInputManager.IsShiftPressed() && itemSlot.Item != null)
			{
				RequestExamineMessage.Send(itemSlot.Item.GetComponent<NetworkIdentity>().netId);
				return;
			}

			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			// if there is an item in this slot, try interacting.
			itemSlot.TryItemInteract();
		}


		public void OnDrag(PointerEventData data)
		{
			if (data.button == PointerEventData.InputButton.Left && itemSlot.Item != null)
			{
				UIManager.UiDragAndDrop.UI_ItemDrag(itemSlot);
			}
		}

		private void Awake()
		{
			itemSlot = GetComponentInChildren<UI_ItemSlot>();
		}

		public new void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			try
			{
				var item = PlayerManager.LocalPlayerScript?.DynamicItemStorage?.GetActiveHandSlot()?.Item;
				if (item == null
				    || itemSlot.Item != null
				    || itemSlot.NamedSlot == NamedSlot.rightHand
				    || itemSlot.NamedSlot == NamedSlot.leftHand)
				{
					return;
				}

				itemSlot.UpdateImage(item.gameObject,
					Validations.CanPutItemToSlot(PlayerManager.LocalPlayerScript, itemSlot.ItemSlot, item,
						NetworkSide.Client)
						? successOverlayColor
						: failOverlayColor);
			}
			catch (NullReferenceException exception)
			{
				Loggy.LogError($"Caught an NRE in UI_ItemSLot.OnPointerEnter() {exception.Message} \n {exception.StackTrace}", Category.UI);
			}
		}

		public new void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);

			itemSlot.UpdateImage(null);
		}

		public void OnDrop(PointerEventData data)
		{
			// something was dropped onto this slot
			if (UIManager.UiDragAndDrop.FromSlotCache != null && UIManager.UiDragAndDrop.DraggedItem != null)
			{
				var fromSlot = UIManager.UiDragAndDrop.DraggedItem.GetComponent<Pickupable>().ItemSlot;
				var targetItem = itemSlot.ItemSlot.ItemObject;

				if (fromSlot.ItemObject == null || fromSlot.ItemObject == targetItem)
					return;

				// if there's an item in the target slot, try inventory apply interaction

				if (targetItem != null)
				{
					var invApply = InventoryApply.ByLocalPlayer(itemSlot.ItemSlot, fromSlot);

					// check interactables in the fromSlot (if it's occupied)
					var fromInteractables = fromSlot.ItemObject.GetComponents<IBaseInteractable<InventoryApply>>()
						.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
					if (InteractionUtils.ClientCheckAndTrigger(fromInteractables, invApply) != null)
					{
						UIManager.UiDragAndDrop.DropInteracted = true;
						UIManager.UiDragAndDrop.StopDrag();
						return;
					}

					// check interactables in the target
					var targetInteractables = targetItem.GetComponents<IBaseInteractable<InventoryApply>>()
						.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
					if (InteractionUtils.ClientCheckAndTrigger(targetInteractables, invApply) != null)
					{
						UIManager.UiDragAndDrop.DropInteracted = true;
						UIManager.UiDragAndDrop.StopDrag();
						return;
					}
				}
				else
				{
					UIManager.UiDragAndDrop.DropInteracted = true;
					UIManager.UiDragAndDrop.StopDrag();
					Inventory.ClientRequestTransfer(fromSlot, itemSlot.ItemSlot);
				}
			}
			UIManager.UiDragAndDrop.StopDrag();
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			// dragging this slot ended somewhere
			UIManager.UiDragAndDrop.StopDrag();
		}
	}
}
