using System.Collections.Generic;
using UnityEngine;
using US13.Objects.Machines;
using US13.UI.Core;
using US13.UI.Core.Net.Page;

namespace US13.UI.Objects.Common.Autolathe
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
