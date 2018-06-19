using System;
using UnityEngine;
using Util;
/// prefab-based for now
/// all server only
public class ItemList : NetUIDynamicList {

	public bool AddItem( string prefabName ) {
		foreach ( DynamicEntry item in Entries.Values ) {
			if ( String.Equals( ( (ItemEntry) item )?.Prefab.ExpensiveName(), prefabName,
				StringComparison.CurrentCultureIgnoreCase ) ) 
			{
				Debug.Log( $"Item {prefabName} already exists in ItemList" );
				return false;
			}
		}
		//load prefab
		GameObject prefab = Resources.Load( prefabName ) as GameObject;
		if ( !prefab || !prefab.GetComponent<ItemAttributes>() ) {
			Debug.LogWarning( $"No valid prefab found: {prefabName}" );
			return false;
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
		NetworkTabManager.Instance.Rescan( MasterTab.NetworkTab );
		UpdatePeepers(); 
		
		return true;
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