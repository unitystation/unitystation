using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabCategoryEntry : DynamicEntry
{
	private MachineProductList exoFabProducts = null;

	public MachineProductList ExoFabProducts
	{
		get => exoFabProducts;
		set => exoFabProducts = value;
	}

	public void OpenCategory()
	{
		Logger.Log("CLICK");
	}

	public void ReInit(MachineProductList productCategory)
	{
		ExoFabProducts = productCategory;
		foreach (var element in Elements)
		{
			string nameBeforeIndex = element.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "CategoryName":
					element.SetValue = ExoFabProducts.CategoryName;
					break;
			}
		}
	}
}