using System;
using Items;
using UnityEngine;

/// <summary>
/// For storing Prefabs and not actual instances
/// To be renamed into PrefabList
/// All methods are serverside.
/// </summary>
public class ItemList : NetUIDynamicList {

	public bool AddItem( string prefabName ) {
		GameObject prefab = Resources.Load( prefabName ) as GameObject;
		return AddItem( prefab );

	}

	public bool AddItem( GameObject prefab )
	{
		if ( !prefab || prefab.GetComponent<ItemAttributesV2>() != null ) {
			Logger.LogWarning( $"No valid prefab found: {prefab}",Category.ItemSpawn );
			return false;
		}

		var entryArray = Entries;
		for ( var i = 0; i < entryArray.Length; i++ ) {
			DynamicEntry entry = entryArray[i];
			var item = entry as ItemEntry;
			if ( !item || !item.Prefab || item.Prefab.Equals( prefab ) ) {
				Logger.Log( $"Item {prefab} already exists in ItemList",Category.ItemSpawn );
				return false;
			}
		}

		//add new entry
		ItemEntry newEntry = Add() as ItemEntry;
		if ( !newEntry ) {
			Logger.LogWarning( $"Added {newEntry} is not an ItemEntry!",Category.ItemSpawn );
			return false;
		}
		//set its elements
		newEntry.Prefab = prefab;
		Logger.Log( $"ItemList: Item add success! newEntry={newEntry}",Category.ItemSpawn );

		//rescan elements  and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		UpdatePeepers();

		return true;
	}

	public bool RemoveItem( GameObject prefab ) {
		return RemoveItem( prefab.name );
	}
	public bool RemoveItem( string prefabName ) {
		foreach ( var pair in EntryIndex ) {
			if ( String.Equals( ( (ItemEntry) pair.Value )?.Prefab.name, prefabName,
				StringComparison.CurrentCultureIgnoreCase ) )
			{
				Remove( pair.Key );
				return true;
			}
		}
		Logger.LogWarning( $"Didn't find any prefabs called '{prefabName}' in the list",Category.ItemSpawn);
		return false;
	}
}