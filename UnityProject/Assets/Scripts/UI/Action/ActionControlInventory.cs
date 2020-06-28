using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Mirror;

public class ActionControlInventory : MonoBehaviour, IClientInventoryMove
{
	public ActionController ActionControllerType = ActionController.Inventory;

	public List<IActionGUI> ControllingActions = new List<IActionGUI>();

	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if(PlayerManager.LocalPlayerScript == null) return;
		var pna = PlayerManager.LocalPlayerScript.playerNetworkActions;
		var showAlert = pna.GetActiveHandItem() == gameObject ||
		                pna.GetOffHandItem() == gameObject;
		foreach (var _IActionGUI in ControllingActions)
		{
			UIActionManager.ToggleLocal(_IActionGUI, showAlert);
		}
	}

	void Start()
	{
		var ActionGUIs = this.GetComponents<IActionGUI>();
		foreach (var ActionGUI in ActionGUIs) {
			if (!ActionGUI.ActionData.PreventBeingControlledBy.Contains(ActionControllerType)) {
				ControllingActions.Add(ActionGUI);
			}
		}
	}
}

