using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoCategory : DynamicEntry
	{
		private CargoCategory category;

		[SerializeField] private NetText_label CategoryName;

		public GUI_Cargo cargoGUI;

		public void OpenCategory()
		{
			cargoGUI.pageSupplies.cargoCategory = category;
			cargoGUI.OpenTab(cargoGUI.pageSupplies);
		}

		public void SetValues(CargoCategory newCategory)
		{
			category = newCategory;
			CategoryName.MasterSetValue(category.CategoryName);
		}
	}
}
