using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_AutolatheCategoryEntry : DynamicEntry
{
	private GUI_Autolathe autolatheMasterTab = null;
	private MachineProductList exoFabProducts = null;

	public MachineProductList ExoFabProducts
	{
		get => exoFabProducts;
		set => exoFabProducts = value;
	}

	public void OpenCategory()
	{
		if (autolatheMasterTab == null) { MasterTab.GetComponent<GUI_Autolathe>().OnCategoryClicked.Invoke(ExoFabProducts); }
		else { autolatheMasterTab?.OnCategoryClicked.Invoke(ExoFabProducts); }
	}

	public void AddAllProducts()
	{
		//Not implemented yet
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
					((NetUIElement<string>)element).SetValueServer(ExoFabProducts.CategoryName);
					break;
			}
		}
	}
}