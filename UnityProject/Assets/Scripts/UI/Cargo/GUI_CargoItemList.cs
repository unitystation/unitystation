using System;
using System.Collections.Generic;
using UnityEngine;

//TODO - delete this class once generic DynamicList arrives
public class GUI_CargoItemList : NetUIDynamicList
{
	public void AddItems(int count)
	{
		for (int i = 0; i < count; i++)
		{
			AddItem();
		}
	}

	public bool AddItem()
	{
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
		Logger.Log($"ItemList: Item add success! newEntry={newEntry}", Category.ItemSpawn);

		//rescan elements  and notify
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
		UpdatePeepers();
		Logger.LogWarning($"Didn't find order '{order.OrderName}' in the list", Category.ItemSpawn);
		return false;
	}
}
