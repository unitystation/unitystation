using System.Collections.Generic;
using UnityEngine;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkItem : NetPage, IPageLifecycle
	{
		[SerializeField]
		private GUI_PDAUplinkMenu controller = null;

		[SerializeField]
		private EmptyItemList dynamicList = null;

		public void OnPageActivated()
		{
			controller.SetBreadcrumb($"{controller.UPLINK_DIRECTORY}/categories/");
		}

		public void OnPageDeactivated()
		{
			ClearItems();
		}

		public void GenerateEntries(UplinkCategory category)
		{
			controller.SetBreadcrumb($"{controller.UPLINK_DIRECTORY}/categories/{category.CategoryName}/");
			var isNukie = controller.mainController.PDA.IsNukeOps;
			foreach (var uplinkItem in category.ItemList)
			{
				if (isNukie || uplinkItem.IsNukeOps == false)
				{
					var entry = dynamicList.AddItem();
					entry.GetComponent<GUI_PDAUplinkItemTemplate>().ReInit(uplinkItem);
				}
			}
		}

		public void SelectItem(UplinkItem item)
		{
			controller.mainController.PDA.SpawnUplinkItem(item.Item, item.Cost);
			controller.UpdateTCCounter();
		}

		private void ClearItems()
		{
			dynamicList.Clear();
		}
	}
}
