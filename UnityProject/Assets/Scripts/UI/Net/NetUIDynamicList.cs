using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

/// These methods are supposed to be run on server
public class NetUIDynamicList : NetUIElement {
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	private int entryCount = 0;

//	[TooltipAttribute("Ones to be initialized from editor, non-runtime")]
//	public List<DynamicEntry> entries = new List<DynamicEntry>();
//	public Dictionary<string,DynamicEntry> Entries = new Dictionary<string,DynamicEntry>(); 
	public Dictionary<string,DynamicEntry> Entries {
		get {
			var dynamicEntries = new Dictionary<string,DynamicEntry>();
			DynamicEntry[] entries = GetComponentsInChildren<DynamicEntry>(true);
			for ( var i = 0; i < entries.Length; i++ ) {
				DynamicEntry entry = entries[i];
				string entryName = entry.name;
				if ( dynamicEntries.ContainsKey( entryName ) ) {
					Debug.LogWarning( $"Duplicate entry name {entryName}, something's wrong" );
					continue;
				}
				dynamicEntries.Add( entryName, entry );
			}

			return dynamicEntries;
		}
	}
	
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
//	private void InitEntries(Dictionary<string,DynamicEntry> entries) {
//		foreach ( var pair in entries ) {
//			var entry = pair.Value;
//			if ( !entry ) {
//				continue;
//			}
//			//Making inner elements' names unique by adding "index" to the end
//			for ( var i = 0; i < entry.Elements.Count; i++ ) {
//				//postfix and not prefix because of how NetKeyButton works
//				entry.Elements[i].name = entry.Elements[i].name + "_" + pair.Key;
//			}
//			//Initializing entry
//			entry.Init();
//			
//		}
//	}

	/// Non-runtime static init
	public override void Init() {
		foreach ( var value in Entries.Values ) {
			InitEntry( value );
		}
	}

	//		for ( var i = 0; i < Entries.; i++ ) {
//			if ( Entries.ContainsKey( i.ToString() ) ) {
//				Debug.Log( $"{transform.parent.parent.name} not adding existing entry" );
//				continue;
//			}
//			InitEntry( Entries[i] );
//		}

	public override string ToString() {
		return Value;
	}

	//"0,1,2,3,4,5,6,7,8,9"

	private void Remove( string toBeRemoved ) {
		Debug.Log( $"Destroying entry #{toBeRemoved}({Entries[toBeRemoved]})" );
		Destroy( Entries[toBeRemoved] );
//		Entries.Remove( toBeRemoved );
	}


	//fixme: sorting's gonna be bad
//todo: support more than one kind per tab

	/// Server adds without providing name
	public DynamicEntry Add( string indexName = "" ) 
	{
		string elementType = $"{MasterTab.Type}Entry";
		
		GameObject entryObject = Instantiate( Resources.Load<GameObject>( elementType ), transform, true );
		entryObject.transform.position = Vector3.down * 70 * Entries.Count;

		DynamicEntry dynamicEntry = entryObject.GetComponent<DynamicEntry>();
		string result = InitEntry( dynamicEntry, indexName );
//		Entries.Add( toBeAdded, entryObject.GetComponent<DynamicEntry>() );
		if ( result != "" ) {
			Debug.Log( $"Spawning entry #[{result}]: proposed: [{indexName}], entry: {dynamicEntry}" );
		} else {
			Debug.LogWarning( $"Entry \"{indexName}\" spawn failure, no such entryObject {elementType}" );
		}

		return dynamicEntry;
	}

	protected virtual string InitEntry( DynamicEntry entry, string desiredName = "" ) {
		if ( !entry ) {
			return "";
		}

		string index = desiredName;
		if ( desiredName == "" ) {
			index = entryCount++.ToString();
			entry.name = index;
		}

		//Making inner elements' names unique by adding "index" to the end
		for ( var i = 0; i < entry.Elements.Count; i++ ) {
			//postfix and not prefix because of how NetKeyButton works
			entry.Elements[i].name = entry.Elements[i].name + "_" + index; 
		}
		//Initializing entry
		entry.Init();

		return index;
	}

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
				Remove( toBeRemoved );
			}
			
			foreach ( string toBeAdded in toAdd ) {
				Add( toBeAdded );
			}
			//Client only:
			if ( ControlTabs.Instance.openedTabs.ContainsKey( MasterTab.NetworkTab ) ) {
				ControlTabs.Instance.openedTabs[MasterTab.NetworkTab]?.RescanElements();
			}
			//Server only:
//			NetworkTabManager.Instance.ReInit(MasterTab.NetworkTab);
			externalChange = false;
		}
	}

	public override void ExecuteServer() {
		throw new System.NotImplementedException();
	}
}