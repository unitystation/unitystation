using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_PDAUplinkItemTemplate : DynamicEntry
{
	private GUI_PDA pdaMasterTab = null;

	[SerializeField]
	private NetLabel itemName;

	[SerializeField]
	private NetLabel itemCost;

	private UplinkItems item;



	public void SelectItem()
	{
		if (pdaMasterTab == null) { pdaMasterTab.GetComponent<GUI_PDA>().OnItemClickedEvent.Invoke(item); }
		else { pdaMasterTab.OnItemClickedEvent.Invoke(item); }
	}


	public void ReInit(UplinkItems assignedItem)
	{
		item = assignedItem;
		itemName.Value = item.Name;
		itemCost.Value = $"Cost {item.Cost} TC";
	}
}
