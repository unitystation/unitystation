using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_ExoFabCategoryEntry : DynamicEntry
{
	private GUI_ExosuitFabricator ExoFabMasterTab = null;
	private MachineProductList exoFabProducts = null;

	public MachineProductList ExoFabProducts
	{
		get => exoFabProducts;
		set => exoFabProducts = value;
	}

	public void OpenCategory()
	{
		if (ExoFabMasterTab == null) { MasterTab.GetComponent<GUI_ExosuitFabricator>().OnCategoryClicked.Invoke(ExoFabProducts); }
		else { ExoFabMasterTab?.OnCategoryClicked.Invoke(ExoFabProducts); }
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