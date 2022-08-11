using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Action
{
	public class ActionControlInventory : MonoBehaviour, IServerInventoryMove
	{
		public ActionController ActionControllerType = ActionController.Inventory;

		public List<IActionGUI> ControllingActions = new List<IActionGUI>();


		public Mind PreviouslyOn;

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

				if (showAlert == false && PreviouslyOn != null)
				{
					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(PreviouslyOn, _IActionGUI, false);
						PreviouslyOn = null;
					}
				}

				if (showAlert == true && PreviouslyOn == null)
				{
					foreach (var _IActionGUI in ControllingActions)
					{
						UIActionManager.ToggleServer(info.ToPlayer.PlayerScript.mind, _IActionGUI, true);
						PreviouslyOn = info.ToPlayer.PlayerScript.mind;
					}
				}
			}
			else if (PreviouslyOn != null)
			{
				foreach (var _IActionGUI in ControllingActions)
				{
					UIActionManager.ToggleServer(PreviouslyOn, _IActionGUI, false);
					PreviouslyOn = null;
				}
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
