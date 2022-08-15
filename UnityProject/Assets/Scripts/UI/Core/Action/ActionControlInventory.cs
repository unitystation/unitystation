using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Action
{
	public class ActionControlInventory : MonoBehaviour, IServerInventoryMove, IOnPlayerLeaveBody, IOnPlayerTransfer
	{
		public ActionController ActionControllerType = ActionController.Inventory;

		public List<IActionGUI> ControllingActions = new List<IActionGUI>();

		private Mind previousMind;

		public void OnInventoryMoveServer(InventoryMove info)
		{
			bool showAlert = false;
			if (info.ToPlayer != null)
			{
				foreach (var itemSlot in info.ToPlayer.PlayerScript.DynamicItemStorage.GetHandSlots())
				{
					if (itemSlot.ItemObject == gameObject)
					{
						showAlert = true;
					}
				}

				if (showAlert == false && previousMind != null)
				{
					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(previousMind, _IActionGUI, false);
					}

					previousMind = null;
				}

				if (showAlert == true && previousMind == null)
				{
					previousMind = info.ToPlayer.PlayerScript.mind;

					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(previousMind, _IActionGUI, true);
					}
				}
			}
			else if (previousMind != null)
			{
				foreach (var _IActionGUI in ControllingActions)
				{
					UIActionManager.ToggleServer(previousMind, _IActionGUI, false);
				}

				previousMind = null;
			}
		}

		void Start()
		{
			var ActionGUIs = this.GetComponents<IActionGUI>();
			foreach (var ActionGUI in ActionGUIs)
			{
				if (ActionGUI.ActionData.PreventBeingControlledBy.Contains(ActionControllerType) == false)
				{
					ControllingActions.Add(ActionGUI);
				}
			}
		}

		public void OnPlayerLeaveBody(Mind mind)
		{
			foreach (var _IActionGUI in ControllingActions)
			{
				UIActionManager.ToggleServer(mind, _IActionGUI, false);
			}

			previousMind = null;
		}

		public void OnPlayerTransfer(Mind mind)
		{
			foreach (var _IActionGUI in ControllingActions)
			{
				UIActionManager.ToggleServer(mind, _IActionGUI, true);
			}

			previousMind = mind;
		}
	}
}
