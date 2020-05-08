using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_PDAUplinkCategoryTemplate : DynamicEntry
{
	private GUI_PDA pdaMasterTab = null;
	private UplinkCatagories category = null;

	[SerializeField]
	private NetLabel categoryName;



	public void OpenCategory()
	{
		if (pdaMasterTab == null) { pdaMasterTab.GetComponent<GUI_PDA>().OnCategoryClickedEvent.Invoke(category); }
		else { pdaMasterTab.OnCategoryClickedEvent.Invoke(category); }
	}


	public void ReInit(UplinkCatagories assignedcategory)
	{
		category = assignedcategory ;
		categoryName.SetValueServer(category.CategoryName);
	}
}
