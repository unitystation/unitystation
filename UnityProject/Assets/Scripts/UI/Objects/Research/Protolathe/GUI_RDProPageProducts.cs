using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_RDProPageProducts : NetPage
	{
		[SerializeField]
		private EmptyItemList productList = null;

		public void DisplayProducts(List<string> AvailableForMachine)
		{
			List<string> products = AvailableForMachine;
			productList.Clear();
			productList.AddItems(products.Count);
			for (int i = 0; i < products.Count; i++)
			{
				GUI_RDProItem item = productList.Entries[i] as GUI_RDProItem;
				item.Product = products[i];
			}
		}
	}
}
