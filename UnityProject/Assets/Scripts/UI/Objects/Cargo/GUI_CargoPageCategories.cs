using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Cargo;

namespace UI.Objects.Cargo
{
	public class GUI_CargoPageCategories : GUI_CargoPage
	{
		[SerializeField]
		private EmptyItemList orderList;

		public override void UpdateTab()
		{
			var categories = CargoManager.Instance.Supplies;

			orderList.Clear();
			orderList.AddItems(categories.Count);
			for (var i = 0; i < categories.Count; i++)
			{
				var item = (GUI_CargoCategory)orderList.Entries[i];
				item.cargoGUI = cargoGUI;
				item.SetValues(categories[i]);
			}
		}
	}
}
