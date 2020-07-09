using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
	{
		private GUI_PDA mainTab;

		private UplinkCatagories category;

		[SerializeField]
		private NetLabel categoryName;


		public void OpenCategory()
		{
			mainTab.OnCategoryClickedEvent.Invoke(category.ItemList);
		}


		public void ReInit(UplinkCatagories assignedCategory)
		{
			mainTab = MasterTab.GetComponent<GUI_PDA>();
			category = assignedCategory;
			categoryName.SetValueServer(category.CategoryName);
		}
	}
}
