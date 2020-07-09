using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkCategory : NetPage
	{
		[SerializeField]
		private GUI_PDAUplinkMenu controller;

		[SerializeField]
		private EmptyItemList categoryTemplate;

		private UplinkCategoryList categories;
		public void UpdateCategory()
		{
			if (categories == null)
			{
				categories = UplinkCategoryList.Instance;
			}
			categoryTemplate.Clear();
			categoryTemplate.AddItems(categories.ItemCategoryList.Count);
			for (int i = 0; i < categories.ItemCategoryList.Count; i++)
			{
				categoryTemplate.Entries[i].GetComponent<GUI_PDAUplinkCategoryTemplate>().ReInit(categories.ItemCategoryList[i]);
			}
		}
		public void ClearCategory()
		{
			categoryTemplate.Clear();
		}

		public void Back()
		{
			ClearCategory();
			controller.mainController.OpenSettings();
		}

		public void Lock()
		{
			Back();
			controller.mainController.Pda.LockUplink();
		}
	}
}
