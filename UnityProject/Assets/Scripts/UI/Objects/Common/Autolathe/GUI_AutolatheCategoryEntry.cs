using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_AutolatheCategoryEntry : DynamicEntry
	{
		private GUI_Autolathe autolatheMasterTab = null;

		public MachineProductList ExoFabProducts { get; set; } = null;

		public void OpenCategory()
		{
			if (autolatheMasterTab == null)
			{
				MasterTab.GetComponent<GUI_Autolathe>().OnCategoryClicked.Invoke(ExoFabProducts);
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
					(element as NetUIElement<string>).SetValueServer(GetName(element));
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
