using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
{
	private GUI_PDA masterTab;

	[NonSerialized]
	public UplinkCatagories Category;

	[SerializeField]
	private NetLabel categoryName;


	public void OpenCategory()
	{
		masterTab.OnCategoryClickedEvent.Invoke(Category.ItemList);
	}


	public void ReInit(UplinkCatagories assignedcategory)
	{
		masterTab = MasterTab.GetComponent<GUI_PDA>();
		Category = assignedcategory;
		categoryName.Value = Category.CategoryName;
	}
}
