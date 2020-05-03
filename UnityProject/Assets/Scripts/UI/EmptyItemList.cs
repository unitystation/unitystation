using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This class exists so you can create empty item entries
/// without need for gameobject(like in SpawnedObjectsList).
/// This will be obsolete once generic lists arrive.
/// </summary>
public class EmptyItemList : NetUIDynamicList
{
	public void AddItems(int count)
	{
		for (int i = 0; i < count; i++)
		{
			AttemptAdd();
		}
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();
	}

	public void SetItems(int count)
	{
		while (count > Entries.Length)
		{
			AttemptAdd();
		}

		while (count < Entries.Length)
		{
			Remove(Entries.Last().name);
		}
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();
	}

	public bool AddItem()
	{
		if(!AttemptAdd())
		{
			return false;
		}

		//rescan elements  and notify
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();

		return true;
	}

	private bool AttemptAdd()
	{
		//add new entry
		var newEntry = Add();
		if (!newEntry)
		{
			Logger.LogWarningFormat("Problems adding {0}", Category.ItemSpawn,
				newEntry);
			return false;
		}
		Logger.LogFormat("ItemList: Item add success! newEntry={0}", Category.ItemSpawn,
			newEntry);
		return true;

	}
}
