using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Items.PDA
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
