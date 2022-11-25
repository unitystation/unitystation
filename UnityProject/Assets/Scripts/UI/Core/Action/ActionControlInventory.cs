using System.Collections;
using System.Collections.Generic;
using UI.Core.Action;
using UnityEngine;

namespace UI.Action
{
	public class ActionControlInventory : MonoBehaviour, IServerInventoryMove
	{
		public ActionController ActionControllerType = ActionController.Inventory;

		public List<IActionGUI> ControllingActions = new List<IActionGUI>();

		private GameObject previousOn;

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

				if (showAlert == false && previousOn != null)
				{
					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(info.FromPlayer.gameObject, _IActionGUI, false);
					}

					previousOn = null;
				}

				if (showAlert == true && previousOn == null)
				{
					previousOn = info.ToPlayer.gameObject;

					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(info.ToPlayer.gameObject, _IActionGUI, true);
					}
				}
			}
			else if (previousOn != null)
			{
				foreach (var _IActionGUI in ControllingActions)
				{
					UIActionManager.ToggleServer(info.FromPlayer.gameObject, _IActionGUI, false);
				}

				previousOn = null;
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
	}
}
