using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageSupplies : GUI_CargoPage
	{
		public EmptyItemList orderList;
		public NetLabel categoryText;

		public CargoCategory cargoCategory;

		public override void UpdateTab()
		{
			var supplies = cargoCategory.Orders;

			orderList.Clear();

			foreach (var cargoOrder in supplies)
			{
				if (cargoOrder.EmagOnly == false || cargoGUI.cargoConsole.Emagged)
				{
					orderList.AddItem();
					var item = (GUI_CargoItem)orderList.Entries[orderList.Entries.Length-1];
					item.SetValues(cargoOrder);
				}
			}

			categoryText.SetValueServer(cargoCategory.CategoryName);
		}
	}
}
