using System;
using UnityEngine;
using Util;
/// prefab-based for now
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
		//load prefab, pull IA and sprite info
		//TODO
		//add new entry
		
		//set its elements??
		
		return true;
	}

	public bool RemoveItem( string name ) {
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