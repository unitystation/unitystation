using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Mirror;
using AdminCommands;
using Messages.Client;
using Messages.Client.Interaction;

public class UI_AdminItemSwap : TooltipMonoBehaviour, IPointerClickHandler
{
	private UI_ItemSlot ui_itemSlot;
	public override string Tooltip => ui_itemSlot.NamedSlot.ToString();

	private void Awake()
	{
		ui_itemSlot = GetComponentInChildren<UI_ItemSlot>();
	}

	public void OnPointerClick(BaseEventData eventData)
	{
		OnPointerClick((PointerEventData) eventData);
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
		if (!PlayerList.Instance.IsClientAdmin)
			return;

		//If shift is pressed, don't check anything, just send Examine on contained item if any.
		if (KeyboardInputManager.IsShiftPressed() && ui_itemSlot.Item != null)
		{
			RequestExamineMessage.Send(ui_itemSlot.Item.GetComponent<NetworkIdentity>().netId);
			return;
		}

		var adminHand = AdminManager.Instance.LocalAdminGhostStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
		if (ui_itemSlot.ItemSlot != adminHand)
		{
			if (ui_itemSlot.Item == null)
			{
				if (adminHand.Item)
				{
					AdminInventoryTransferMessage.Send(adminHand, ui_itemSlot.ItemSlot);
				}
			}
			else
			{
				if (adminHand.Item == null)
				{
					AdminInventoryTransferMessage.Send(ui_itemSlot.ItemSlot, adminHand);
				}
			}

		}
	}

}