using UnityEngine;
using Util;

public class ItemList : NetUIDynamicList {
	//todo: hier / prefab name / both?
	public bool AddItem( string prefabName ) {
		foreach ( DynamicEntry item in Entries.Values ) {
			if ( ((ItemEntry) item)?.Prefab.ExpensiveName() == prefabName ) {
				Debug.Log( $"Item {prefabName} already exists in ItemList" );
				return false;
			}
		}
		//load prefab, pull IA and sprite info
		
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