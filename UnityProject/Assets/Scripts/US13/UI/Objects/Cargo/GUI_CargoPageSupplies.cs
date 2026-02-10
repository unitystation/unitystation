using UnityEngine;
using US13.Systems.Cargo;
using US13.UI.Core;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Cargo
{
	public class GUI_CargoPageSupplies : GUI_CargoPage
	{
		[SerializeField]
		private EmptyItemList orderList;
		[SerializeField]
		private NetText_label categoryText;

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

			categoryText.MasterSetValue(cargoCategory.CategoryName);
		}
	}
}
