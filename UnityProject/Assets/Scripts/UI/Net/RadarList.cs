using System.Collections.Generic;
using UnityEngine;

/// all server only
public class RadarList : NetUIDynamicList {
	public int Range = 160;
	public MatrixMove Origin;

	private List<RadarEntry> OutOfRangeEntries = new List<RadarEntry>();
	private List<RadarEntry> ToRestore = new List<RadarEntry>();

	public void RefreshTrackedPos() {
		Vector2 originPos = Origin.State.Position;
		
		//Refreshing positions of every item
		var entryArray = EntryArray;
		for ( var i = 0; i < entryArray.Length; i++ ) {
			var item = entryArray[i] as RadarEntry;
			if ( !item ) {
				continue;
			}

			item.RefreshTrackedPos(originPos);
			//If item is out of range, stop showing it and place into "out of range" list
			if ( ProjectionMagnitude( item ) > Range )
			{
				Debug.Log( $"Hiding {item} as it's out of range" );
				OutOfRangeEntries.Add( item );
				item.gameObject.SetActive( false );
			}
		}
		//Check if any item in "out of range" list should be shown again
		for ( var i = 0; i < OutOfRangeEntries.Count; i++ ) {
			var item = OutOfRangeEntries[i];
			item.RefreshTrackedPos( originPos );
			if ( ProjectionMagnitude( item ) <= Range )
			{
				Debug.Log( $"Unhiding {item} as it's in range again" );
				ToRestore.Add( item );
				item.gameObject.SetActive( true );
			}
		}
		
		for ( var i = 0; i < ToRestore.Count; i++ ) {
			var item = ToRestore[i];
			OutOfRangeEntries.Remove( item );
		}

		ToRestore.Clear();

		UpdatePeepers();
	}

	/// For square radar. For round radar item.Position.magnitude check should suffice.
	private static float ProjectionMagnitude( RadarEntry item ) {
		var pos = item.Position;
		var projX = Vector3.Project( pos, Vector3.right ).magnitude;
		var projY = Vector3.Project( pos, Vector3.up ).magnitude;
		return projX >= projY ? projX : projY;
	}

	//do we need to bulk add static positions? don't think so
	public bool AddStaticItem( MapIconType type, Vector2 staticPosition, int radius = -1 ) { 
		for ( var i = 0; i < EntryArray.Length; i++ ) {
			var item = EntryArray[i] as RadarEntry;
			if ( !item ) {
				continue;
			}

			if ( staticPosition == (Vector2) item.StaticPosition ) {
				return false;
			}
		}
			//add new entry
			RadarEntry newEntry = Add() as RadarEntry;
			if ( !newEntry ) {
				Debug.LogWarning( $"Added {newEntry} is not an RadarEntry!" );
				return false;
			}

			//set its elements
			newEntry.Radius = radius;
			newEntry.Type = type;
			newEntry.StaticPosition = staticPosition;
		
		//rescan elements and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		RefreshTrackedPos();
		
		return true;
	}

	public bool AddItems( MapIconType type, List<GameObject> objects ) 
	{
		var objectSet = new HashSet<GameObject>(objects);
		var duplicates = new HashSet<GameObject>();
		for ( var i = 0; i < EntryArray.Length; i++ ) {
			var item = EntryArray[i] as RadarEntry;
			if ( !item ) {
				continue;
			}

			if ( objectSet.Contains( item.TrackedObject ) ) {
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
			newEntry.TrackedObject = obj;
		}
//		Debug.Log( $"RadarList: Item add success! added {objects.Count} items" );
		
		//rescan elements and notify
		NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		RefreshTrackedPos();
		
		return true;
	}
	//todo RemoveTrackedObject(s)

	//Don't apply any clientside ordering and just rely on whatever server provided
	protected override void RefreshPositions() {}
	protected override void SetProperPosition( DynamicEntry entry, int index = 0 ) {}
}