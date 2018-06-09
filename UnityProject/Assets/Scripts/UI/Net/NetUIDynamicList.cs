using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

/// These methods are supposed to be run on server
public class NetUIDynamicList : NetUIElement {
	public override ElementMode InteractionMode => ElementMode.ServerWrite;

	[Tooltip("Doesn't support adding/removing in runtime from Inspector")]
	public List<DynamicEntry> entries = new List<DynamicEntry>();
	public Dictionary<string,DynamicEntry> Entries = new Dictionary<string,DynamicEntry>(); 
//	TODO consider => GetComponentsInChildren<DynamicEntry>(true).foreach collect (x) => new Pair<>(x.gameObject.name, x)
	
//	public List<DynamicEntry> entries = new List<DynamicEntry>();
	//on serverside update/add/remove: notify players and reinit

//	public void AddEntry(DynamicEntry entry) {
//		if ( !Entries.ContainsValue( entry ) ) {
//			Entries.Add( Entries.Count.ToString(), entry );
//			MakeElementsUnique( entry );
//			//notify, reinit
//		}
//	}
//	public void RemoveEntry(DynamicEntry entry) {
//		if ( Entries.ContainsValue( entry ) ) {
//			//yep, removing this way is expensive
//			var item = Entries.First(kvp => kvp.Value == entry);
//			Entries.Remove(item.Key);
//			//notify, reinit
//			NetworkTabManager.Instance.ReInit(MasterTab.NetworkTab);
//		}
//	}
//todo: new method to init newly added entry
	private void InitEntries(Dictionary<string,DynamicEntry> entries) {
		foreach ( var pair in entries ) {
			var entry = pair.Value;
			if ( !entry ) {
				continue;
			}
			//Making inner elements' names unique by adding "index" to the end
			for ( var i = 0; i < entry.Elements.Count; i++ ) {
				//postfix and not prefix because of how NetKeyButton works
				entry.Elements[i].name = entry.Elements[i].name + "_" + pair.Key; //fixme: wring index if adding in runtime
			}
			//Initializing entry
			entry.Init();
			
		}
	}

	public override void Init() {
		for ( var i = 0; i < entries.Count; i++ ) {
			if ( !entries[i] ) { //skippping nulls
				continue;
			}
			if ( Entries.ContainsKey( i.ToString() ) ) {
				Debug.Log( $"{transform.parent.parent.name} not adding existing dynamic entry" );
				continue;
			}

			Entries.Add( i.ToString(), entries[i] );
		}
		InitEntries(Entries);
	}

	public override string ToString() {
		return Value;
	}
	//"0,1,2,3,4,5,6,7,8,9"
	
	public override string Value {
		get { return string.Join( ",", Entries.Keys ); }
		set {
			externalChange = true;
			var proposed = value.Split( new[]{','} , StringSplitOptions.RemoveEmptyEntries).ToList();
			
			//add ones existing in proposed only, remove ones not existing in proposed
			//could probably be cheaper
			var toRemove = Entries.Keys.Except( proposed );
			var toAdd = proposed.Except( Entries.Keys );
			
			foreach ( string toBeRemoved in toRemove ) {
				Debug.Log( $"Destroying entry #{toBeRemoved}({Entries[toBeRemoved]})" );
				Destroy(Entries[toBeRemoved]);
				Entries.Remove( toBeRemoved );
			}
			
			foreach ( string toBeAdded in toAdd ) {
				//todo: support more than one kind per tab
				var entryObject = Instantiate( Resources.Load( $"{MasterTab.Type}Entry" ) as GameObject, transform, true );
				entryObject.transform.position = Vector3.down * 70 * Entries.Count;
				Entries.Add( toBeAdded, entryObject.GetComponent<DynamicEntry>() );
				Debug.Log( $"Spawning entry #[{toBeAdded}]: [{Entries[toBeAdded]}]" );
			}
			//Client only:
//			ControlTabs.Instance.openedTabs[MasterTab.NetworkTab].ReInitElements();
			//Server only:
//			NetworkTabManager.Instance.ReInit(MasterTab.NetworkTab);
			externalChange = false;
		}
	}

	public override void ExecuteServer() {
		throw new System.NotImplementedException();
	}
}