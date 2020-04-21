﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabPageProducts : NetPage
{
	[SerializeField]
	private EmptyItemList productList;

	public void DisplayProducts(MachineProductList exoFabProducts)
	{
		List<MachineProduct> products = exoFabProducts.Products;
		productList.Clear();
		productList.AddItems(products.Count);
		for (int i = 0; i < products.Count; i++)
		{
			GUI_ExoFabItem item = productList.Entries[i] as GUI_ExoFabItem;
			item.Product = products[i];
		}
	}
}