using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
	{
		private GUI_PDA masterTab;

		private UplinkCatagories category;

		[SerializeField]
		private NetLabel categoryName;


		public void OpenCategory()
		{
			masterTab.OnCategoryClickedEvent.Invoke(category.ItemList);
		}


		public void ReInit(UplinkCatagories assignedcategory)
		{
			masterTab = MasterTab.GetComponent<GUI_PDA>();
			category = assignedcategory;
			categoryName.Value = category.CategoryName;
		}
	}
}
