using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_PDAUplinkItem : NetPage
{
	[SerializeField]
	private EmptyItemList itemTemplate;

	public void GenerateEntries(List<UplinkItems> itementries)
	{
		itemTemplate.Clear();
		itemTemplate.AddItems(itementries.Count);
		for (int i = 0; i < itementries.Count; i++)
		{
			itemTemplate.Entries[i].GetComponent<GUI_PDAUplinkItemTemplate>().ReInit(itementries[i]);
		}
	}
}
