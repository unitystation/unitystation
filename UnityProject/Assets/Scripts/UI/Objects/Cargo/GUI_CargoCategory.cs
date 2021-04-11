using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoCategory : DynamicEntry
	{
		private CargoCategory category;

		public NetLabel CategoryName;
		public GUI_Cargo cargoGUI;

		public void OpenCategory()
		{
			cargoGUI.pageSupplies.cargoCategory = category;
			cargoGUI.OpenTab(cargoGUI.pageSupplies);
		}

		public void SetValues(CargoCategory newCategory)
		{
			category = newCategory;
			CategoryName.SetValueServer(category.CategoryName);
		}
	}
}
