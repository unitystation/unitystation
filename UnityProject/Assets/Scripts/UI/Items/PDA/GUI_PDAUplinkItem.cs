using System.Collections.Generic;
using UnityEngine;

namespace UI.PDA
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
			dynamicList.AddItems(category.ItemList.Count);
			for (int i = 0; i < category.ItemList.Count; i++)
			{
				dynamicList.Entries[i].GetComponent<GUI_PDAUplinkItemTemplate>().ReInit(category.ItemList[i]);
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
