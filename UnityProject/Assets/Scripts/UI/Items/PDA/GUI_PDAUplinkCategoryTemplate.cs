using System;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Items.PDA
{
	public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
	{
		[SerializeField]
		private NetText_label categoryName = null;

		private GUI_PDAUplinkCategory categoryPage;
		private UplinkCategory category;

		public void OpenCategory()
		{
			categoryPage.OpenUplinkCategory(category);
		}

		public void ReInit(UplinkCategory assignedCategory)
		{
			categoryPage = containedInTab.GetComponent<GUI_PDA>().uplinkPage.categoryPage;
			category = assignedCategory;
			categoryName.MasterSetValue(category.CategoryName);
		}
	}
}
