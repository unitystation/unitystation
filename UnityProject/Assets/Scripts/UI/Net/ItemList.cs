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
		if ( !prefab ) {
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
		
		newEntry.Init();
		Debug.Log( $"ItemList: Item add success! newEntry={newEntry}" );

		//notify, reinit
		NetworkTabManager.Instance.ReInit( MasterTab.NetworkTab );

		return true;
	}

	public bool RemoveItem( string name ) { //todo
//		if ( Entries.ContainsValue( entry ) ) {
//			//yep, removing this way is expensive
//			var item = Entries.First( kvp => kvp.Value == entry );
//			Entries.Remove( item.Key );
//			//notify, reinit
//			NetworkTabManager.Instance.ReInit( MasterTab.NetworkTab );
//		}
		return false;
	}
}