using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Util;
/// all server only
public class RadarList : NetUIDynamicList {
	public bool AddItem( MapIconType type, Vector2 position ) {
		return AddItems( type, new[] {position} );
	}

	public bool AddItems( MapIconType type, Vector2[] positions ) 
	{
		var positionSet = new HashSet<Vector2>(positions);
		var duplicates = new HashSet<Vector2>();
		foreach ( DynamicEntry entry in Entries.Values ) {
			var item = entry as RadarEntry;
			if ( !item ) {
				continue;
			}
			if ( /*item.Type == type &&*/ positionSet.Contains(item.Position)  ) 
			{
//				Debug.Log( $"Item {item} already exists in RadarList" );
				duplicates.Add( item.Position );
			}
		}
		for ( var i = 0; i < positions.Length; i++ ) {
			var position = positions[i];
			//skipping already found positions 
			if ( duplicates.Contains( position ) ) {
				continue;
			}

			//add new entry
			RadarEntry newEntry = Add() as RadarEntry;
			if ( !newEntry ) {
				Debug.LogWarning( $"Added {newEntry} is not an RadarEntry!" );
				return false;
			}

			//set its elements
			newEntry.Type = type;
			newEntry.Position = position;
		}
		Debug.Log( $"RadarList: Item add success! added {positions.Length} items" );

		//rescan elements and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		UpdatePeepers(); 
		
		return true;
	}

	public void ClearItems() { //todo
//		foreach ( var pair in Entries ) {
//			if ( String.Equals( ( (ItemEntry) pair.Value )?.Prefab.name, prefabName,
//				StringComparison.CurrentCultureIgnoreCase ) ) 
//			{
//				Remove( pair.Key );
//				return true;
//			}
//		}
//		Debug.LogWarning( $"Didn't find any prefabs called '{prefabName}' in the list" );
	}

	protected override void RefreshPositions() {}

		//not doing anything, see how DynamicEntry works
	protected override void SetProperPosition( DynamicEntry entry, int index = 0 ) {}
}