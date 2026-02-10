using US13.Objects.Machines;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Common.Autolathe
{
	public class GUI_AutolatheCategoryEntry : DynamicEntry
	{
		private GUI_Autolathe autolatheMasterTab = null;

		public MachineProductList ExoFabProducts { get; set; } = null;

		public void OpenCategory()
		{
			if (autolatheMasterTab == null)
			{
				containedInTab.GetComponent<GUI_Autolathe>().OnCategoryClicked.Invoke(ExoFabProducts);
			}
			else
			{
				autolatheMasterTab?.OnCategoryClicked.Invoke(ExoFabProducts);
			}
		}

		public void AddAllProducts()
		{
			//Not implemented yet
		}

		public void ReInit(MachineProductList productCategory)
		{
			ExoFabProducts = productCategory;
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
				return ExoFabProducts.CategoryName;
			}

			return default;
		}
	}
}
