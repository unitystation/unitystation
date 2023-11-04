using System;
using UnityEngine;
using Items;
using Logs;

namespace UI.Core.NetUI
{
	/// <summary>
	/// For storing Prefabs and not actual instances
	/// To be renamed into PrefabList
	/// All methods are serverside.
	/// </summary>
	public class ItemList : NetUIDynamicList
	{
		public bool AddItem(string prefabName)
		{
			GameObject prefab = CustomNetworkManager.Instance.GetSpawnablePrefabFromName(prefabName);
			return AddItem(prefab);
		}

		public bool AddItem(GameObject prefab)
		{
			if (!prefab || prefab.GetComponent<ItemAttributesV2>() != null)
			{
				Loggy.LogWarning($"No valid prefab found: {prefab}", Category.ItemSpawn);
				return false;
			}

			var entryArray = Entries;
			for (var i = 0; i < entryArray.Count; i++)
			{
				DynamicEntry entry = entryArray[i];
				var item = entry as ItemEntry;
				if (!item || !item.Prefab || item.Prefab.Equals(prefab))
				{
					Loggy.Log($"Item {prefab} already exists in ItemList", Category.ItemSpawn);
					return false;
				}
			}

			//add new entry
			ItemEntry newEntry = Add() as ItemEntry;
			if (newEntry == null)
			{
				Loggy.LogWarning($"Added {newEntry} is not an ItemEntry!", Category.ItemSpawn);
				return false;
			}

			//set its elements
			newEntry.Prefab = prefab;
			Loggy.Log($"ItemList: Item add success! newEntry={newEntry}", Category.ItemSpawn);

			//rescan elements  and notify
			NetworkTabManager.Instance.Rescan(containedInTab.NetTabDescriptor);
			UpdatePeepers();

			return true;
		}

		public bool MasterRemoveItem(GameObject prefab)
		{
			return MasterRemoveItem(prefab.name);
		}

		public bool MasterRemoveItem(string prefabName)
		{
			foreach (var pair in EntryIndex)
			{
				if (string.Equals(((ItemEntry)pair.Value)?.Prefab.name, prefabName,
					StringComparison.CurrentCultureIgnoreCase))
				{
					Remove(pair.Key);
					return true;
				}
			}

			Loggy.LogWarning($"Didn't find any prefabs called '{prefabName}' in the list", Category.ItemSpawn);
			return false;
		}
	}
}
