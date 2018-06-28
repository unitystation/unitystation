using System;
using UnityEngine;
using Util;
/// prefab-based for now
/// all server only
public class ItemList : NetUIDynamicList {

	public bool AddItem( string prefabName ) {
		GameObject prefab = Resources.Load( prefabName ) as GameObject;
		return AddItem( prefab );
		
	}

	public bool AddItem( GameObject prefab ) 
	{
		if ( !prefab || !prefab.GetComponent<ItemAttributes>() ) {
			Debug.LogWarning( $"No valid prefab found: {prefab}" );
			return false;
		}

		var entryArray = EntryArray;
		for ( var i = 0; i < entryArray.Length; i++ ) {
			DynamicEntry entry = entryArray[i];
			var item = entry as ItemEntry;
			if ( !item || !item.Prefab || item.Prefab.Equals( prefab ) ) {
				Debug.Log( $"Item {prefab} already exists in ItemList" );
				return false;
			}
		}

		//add new entry
		ItemEntry newEntry = Add() as ItemEntry;
		if ( !newEntry ) {
			Debug.LogWarning( $"Added {newEntry} is not an ItemEntry!" );
			return false;
		}
		//set its elements
		newEntry.Prefab = prefab;
		Debug.Log( $"ItemList: Item add success! newEntry={newEntry}" );

		//rescan elements  and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		UpdatePeepers(); 
		
		return true;
	}

	public bool RemoveItem( GameObject prefab ) {
		return RemoveItem( prefab.name );
	}
	public bool RemoveItem( string prefabName ) {
		foreach ( var pair in Entries ) {
			if ( String.Equals( ( (ItemEntry) pair.Value )?.Prefab.name, prefabName,
				StringComparison.CurrentCultureIgnoreCase ) ) 
			{
				Remove( pair.Key );
				return true;
			}
		}
		Debug.LogWarning( $"Didn't find any prefabs called '{prefabName}' in the list" );
		return false;
	}
}