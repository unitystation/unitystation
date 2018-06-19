using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

public class NetUIDynamicList : NetUIElement {
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	private int entryCount = 0;
	

	public Dictionary<string,DynamicEntry> Entries {
		get {
			var dynamicEntries = new Dictionary<string,DynamicEntry>();
			DynamicEntry[] entries = GetComponentsInChildren<DynamicEntry>(false);
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

	/// Non-runtime static init
	public override void Init() {
		foreach ( var value in Entries.Values ) {
			InitEntry( value );
		}
	}

	public override string ToString() {
		return Value;
	}

	protected void Remove( string toBeRemoved ) {
		var entryToRemove = Entries[toBeRemoved];
		Debug.Log( $"Destroying entry #{toBeRemoved}({entryToRemove})" );
//		Destroy( Entries[toBeRemoved].gameObject ); 
		entryToRemove.gameObject.SetActive( false );
		NetworkTabManager.Instance.Rescan( MasterTab.NetworkTab );
		
		RefreshPositions();

		if ( CustomNetworkManager.Instance._isServer ) {
			UpdatePeepers();
		}
	}

//todo: support more than one kind per tab

	/// Server adds without providing name
	protected DynamicEntry Add( string indexName = "" ) 
	{
		string elementType = $"{MasterTab.Type}Entry";
		
		GameObject entryObject = Instantiate( Resources.Load<GameObject>( elementType ), transform, false );
		
		DynamicEntry dynamicEntry = entryObject.GetComponent<DynamicEntry>();

		string result = InitEntry( dynamicEntry, indexName );

		RefreshPositions();
		
		if ( result != "" ) {
			Debug.Log( $"Spawning entry #[{result}]: proposed: [{indexName}], entry: {dynamicEntry}" );
		} else {
			Debug.LogWarning( $"Entry \"{indexName}\" spawn failure, no such entryObject {elementType}" );
		}

		return dynamicEntry;
	}

	/// Need to run this on list change to ensure no gaps are present
	public void RefreshPositions() {
		var entries = Entries.Values.ToList();
		for ( var i = 0; i < entries.Count; i++ ) {
			SetProperPosition( entries[i], i );
		}
	}

	/// Defines the way list items are positioned.
	/// Adds next entries directly below (using height) by default
	protected virtual void SetProperPosition( DynamicEntry entry, int index = 0 ) {
		RectTransform rect = entry.gameObject.GetComponent<RectTransform>();
		rect.anchoredPosition = Vector3.down * rect.rect.height * index;
	}

	///Not just own value, include inner elements' values as well
	protected override void UpdatePeepers() {
		List<ElementValue> valuesToSend = new List<ElementValue> {ElementValue};
		
		foreach ( var entry in Entries.Values ) {
			for ( var i = 0; i < entry.Elements.Count; i++ ) {
				var element = entry.Elements[i];
				valuesToSend.Add(element.ElementValue);
			}
		}

		TabUpdateMessage.SendToPeepers( MasterTab.Provider, MasterTab.Type, TabAction.Update, valuesToSend.ToArray() );
	}

	protected virtual string InitEntry( DynamicEntry entry, string desiredName = "" ) {
		if ( !entry ) {
			return "";
		}

		string index = desiredName;
		if ( desiredName == "" ) {
			index = entryCount++.ToString();
		}
		entry.name = index;

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
//			//Client only:
//			if ( ControlTabs.Instance.openedTabs.ContainsKey( MasterTab.NetworkTab ) ) {
//				ControlTabs.Instance.openedTabs[MasterTab.NetworkTab]?.RescanElements();
//			}
			externalChange = false;
		}
	}

	public override void ExecuteServer() {}
}