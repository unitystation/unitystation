using System.Collections.Generic;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Research.Protolathe
{
	public class GUI_RDProCategoryEntry : DynamicEntry
	{
		private GUI_RDProductionMachine rdProMasterTab = null;
		private string CategoryName;
		private List<string> CategoryProducts;

		public void OpenCategory()
		{
			if (rdProMasterTab == null)
			{
				containedInTab.GetComponent<GUI_RDProductionMachine>().OnCategoryClicked.Invoke(CategoryProducts,CategoryName);
			}
			else
			{
				rdProMasterTab?.OnCategoryClicked.Invoke(CategoryProducts, CategoryName);
			}
		}

		public void AddAllProducts()
		{
			//Not implemented yet
		}

		public void ReInit(List<string> categoryProducts, string categoryName)
		{
			CategoryProducts = categoryProducts;
			CategoryName = categoryName;

			foreach (var element in Elements)
			{
				if (( element as NetUIElement<string>) != null)
				{
					(element as NetUIElement<string>).MasterSetValue(GetName(element));
				}
			}
		}

		private string GetName(NetUIElementBase element)
		{
			string nameBeforeIndex = element.name.Split('~')[0];

			if (nameBeforeIndex == "CategoryName")
			{
				return CategoryName;
			}

			return default;
		}
	}
}
