using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
{
	private GUI_PDA pdaMasterTab = null;

	[NonSerialized]
	public UplinkCatagories Category;

	[SerializeField]
	private NetLabel categoryName;



	public void OpenCategory()
	{
		if (pdaMasterTab == null) { pdaMasterTab.GetComponent<GUI_PDA>().OnCategoryClickedEvent.Invoke(Category.ItemList); }
		else { pdaMasterTab.OnCategoryClickedEvent.Invoke(Category.ItemList); }
	}


	public void ReInit(UplinkCatagories assignedcategory)
	{
		Category = assignedcategory;
		categoryName.Value = Category.CategoryName;
	}
}
