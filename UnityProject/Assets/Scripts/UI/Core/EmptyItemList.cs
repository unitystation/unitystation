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
			Add();
		}
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();
	}

	public void SetItems(int count)
	{
		while (count > Entries.Length)
		{
			Add();
		}

		while (count < Entries.Length)
		{
			Remove(Entries.Last().name);
		}
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();
	}

	public DynamicEntry AddItem()
	{
		var newEntry = Add();

		//rescan elements  and notify
		NetworkTabManager.Instance.Rescan(MasterTab.NetTabDescriptor);
		UpdatePeepers();

		return newEntry;
	}

}
