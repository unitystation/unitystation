using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Machines;

namespace UI.Objects
{
	public class GUI_AutolathePageProducts : NetPage
	{
		[SerializeField]
		private EmptyItemList productList = null;

		public void DisplayProducts(MachineProductList autolatheProducts)
		{
			List<MachineProduct> products = autolatheProducts.Products;
			productList.Clear();
			productList.AddItems(products.Count);
			for (int i = 0; i < products.Count; i++)
			{
				GUI_AutolatheItem item = productList.Entries[i] as GUI_AutolatheItem;
				item.Product = products[i];
			}
		}
	}
}
