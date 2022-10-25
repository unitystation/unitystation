using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects.Robotics
{
	public class GUI_ExoFabCategoryEntry : DynamicEntry
	{
		private GUI_ExosuitFabricator ExoFabMasterTab = null;
		private MachineProductList exoFabProducts = null;

		public MachineProductList ExoFabProducts {
			get => exoFabProducts;
			set => exoFabProducts = value;
		}

		public void OpenCategory()
		{
			if (ExoFabMasterTab == null)
			{
				containedInTab.GetComponent<GUI_ExosuitFabricator>().OnCategoryClicked.Invoke(ExoFabProducts);
			}
			else
			{
				ExoFabMasterTab?.OnCategoryClicked.Invoke(ExoFabProducts);
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
				if (element as NetUIElement<string> != null)
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
