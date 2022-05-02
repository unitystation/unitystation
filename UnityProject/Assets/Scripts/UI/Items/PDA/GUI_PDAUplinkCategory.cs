using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkCategory : NetPage, IPageReadyable
	{
		[SerializeField]
		private GUI_PDAUplinkMenu controller = null;
		[SerializeField]
		private EmptyItemList dynamicList = null;

		private bool isInitialised = false;

		public void OnPageActivated()
		{
			if (!isInitialised)
			{
				InitialiseCategories();
			}

			controller.SetBreadcrumb($"{controller.UPLINK_DIRECTORY}/categories/");
		}

		public void OpenUplinkCategory(UplinkCategory category)
		{
			controller.OpenSubPage(controller.itemPage);
			controller.itemPage.GenerateEntries(category);
		}

		private void InitialiseCategories()
		{
			var categories = UplinkCategoryList.Instance.ItemCategoryList;

			isInitialised = true;
			dynamicList.AddItems(categories.Count);
			for (int i = 0; i < categories.Count; i++)
			{
				dynamicList.Entries[i].GetComponent<GUI_PDAUplinkCategoryTemplate>().ReInit(categories[i]);
			}
		}
	}
}
