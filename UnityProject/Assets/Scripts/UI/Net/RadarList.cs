using System.Collections.Generic;
using UnityEngine;

/// all server only
public class RadarList : NetUIDynamicList {
	public void RefreshTrackedPos() { //todo: optimize
		foreach ( DynamicEntry entry in Entries.Values ) {
			var item = entry as RadarEntry;
			if ( !item ) {
				continue;
			}
			item.RefreshTrackedPos();
		}
		UpdatePeepers(); 
	}

	public bool AddItems( MapIconType type, GameObject origin, List<GameObject> objects ) 
	{
		var objectSet = new HashSet<GameObject>(objects);
		var duplicates = new HashSet<GameObject>();
		foreach ( DynamicEntry entry in Entries.Values ) {
			var item = entry as RadarEntry;
			if ( !item ) {
				continue;
			}
			if ( objectSet.Contains(item.TrackedObject)  ) 
			{
				duplicates.Add( item.TrackedObject );
			}
		}
		for ( var i = 0; i < objects.Count; i++ ) {
			var obj = objects[i];
			//skipping already found objects 
			if ( duplicates.Contains( obj ) ) {
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
			newEntry.OriginObject = origin;
			newEntry.TrackedObject = obj;
		}
//		Debug.Log( $"RadarList: Item add success! added {objects.Count} items" );
		
		//rescan elements and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		RefreshTrackedPos();
		
		return true;
	}

	protected override void RefreshPositions() {}

	//not doing anything, see how DynamicEntry works
	protected override void SetProperPosition( DynamicEntry entry, int index = 0 ) {}
}