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
					var item = orderList.AddItem().GetComponent<GUI_CargoItem>();
					item.SetValues(cargoOrder);
				}
			}

			categoryText.SetValueServer(cargoCategory.CategoryName);
		}
	}
}
