using System;
using UnityEngine;

public class GUI_CargoItemList : NetUIDynamicList
{
	public bool AddItem(CargoOrder order)
	{
		if (order == null)
		{
			return false;
		}

		var entryArray = Entries;
		for (var i = 0; i < entryArray.Length; i++)
		{
			DynamicEntry entry = entryArray[i];
			var item = entry as GUI_CargoItem;
		}

		//add new entry
		GUI_CargoItem newEntry = Add() as GUI_CargoItem;
		if (!newEntry)
		{
			Logger.LogWarning($"Added {newEntry} is not an CargoItem!", Category.ItemSpawn);
			return false;
		}
		//set its elements
		newEntry.Order = order;
		Logger.Log($"ItemList: Item add success! newEntry={newEntry}", Category.ItemSpawn);

		//rescan elements  and notify
		Debug.Log("tab " + MasterTab.ToString() + " desc " + MasterTab.NetTabDescriptor.ToString());
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();

		return true;
	}

	public bool RemoveItem(CargoOrder order)
	{
		foreach (var pair in EntryIndex)
		{
			if (((GUI_CargoItem)pair.Value)?.Order == order)
			{
				Remove(pair.Key);
				return true;
			}
		}
		Logger.LogWarning($"Didn't find order '{order.OrderName}' in the list", Category.ItemSpawn);
		return false;
	}
}
