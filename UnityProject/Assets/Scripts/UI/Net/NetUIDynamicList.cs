using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// These methods are supposed to be run on server
public class NetUIDynamicList : NetUIElement {
	public override bool IsNonInteractable => true;

	public Dictionary<string,DynamicEntry> Entries = new Dictionary<string,DynamicEntry>();
//	public List<DynamicEntry> entries = new List<DynamicEntry>();
	//on serverside update/add/remove: notify players and reinit

	
	public void AddEntry(DynamicEntry entry) {
		if ( !Entries.ContainsValue( entry ) ) {
			Entries.Add( Entries.Count.ToString(), entry );
			MakeElementsUnique( entry );
			//notify, reinit
		}
	}
	public void RemoveEntry(DynamicEntry entry) {
		if ( Entries.ContainsValue( entry ) ) {
			//yep, removing this way is expensive
			var item = Entries.First(kvp => kvp.Value == entry);
			Entries.Remove(item.Key);
			//notify, reinit
			NetworkTabManager.Instance.ReInit(MasterTab.NetworkTab);
		}
	}

	private void MakeElementsUnique(DynamicEntry entry) {
		for ( var i = 0; i < entry.Elements.Count; i++ ) {
			//postfix and not prefix because of how NetKeyButton works
			entry.Elements[i].name = entry.Elements[i].name + "_" + name;
		}
	}

	private void Start() {
		//Not doing this for clients
		if ( CustomNetworkManager.Instance._isServer ) {
			foreach ( DynamicEntry entry in Entries.Values ) {
				MakeElementsUnique(entry);
			}
		}
	}
	//"0,1,2,3,4,5,6,7,8,9"
	
	public override string Value {
		get { return string.Join( ",", Entries ); }
		set {
			externalChange = true;
			var proposed = value.Split( ',' ).ToList();
			
			//add ones existing in proposed only, remove ones not existing in proposed
			//could probably be cheaper
			var toRemove = Entries.Keys.Except( proposed );
			var toAdd = proposed.Except( Entries.Keys );
			
			foreach ( string toBeRemoved in toRemove ) {
				Entries.Remove( toBeRemoved );
			}
			
			foreach ( string toBeAdded in toAdd ) {
//				Entries.Add( toBeAdded );//TODO: sort out client and server adding/removing, incl. actual destroy/spawn
			}
			
//			Element.text = value;
			externalChange = false;
		}
	}

	public override void ExecuteServer() {
		throw new System.NotImplementedException();
	}
}