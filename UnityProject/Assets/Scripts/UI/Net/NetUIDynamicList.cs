using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;

/// Base class for List of dynamic entries, which can be added/removed at runtime.
/// Setting Value actually creates/removes entries for client
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
			InitDynamicEntry( value );
		}
	}

	public override string ToString() {
		return Value;
	}

	protected void Remove( string toBeRemoved ) {
		var entryToRemove = Entries[toBeRemoved];
		Debug.Log( $"Destroying entry #{toBeRemoved}({entryToRemove})" );
		entryToRemove.gameObject.SetActive( false );
		
		if ( CustomNetworkManager.Instance._isServer ) {
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
			UpdatePeepers();
		}
		RefreshPositions();
	}

	/// Adds new entry at given index (or generates index if none is provided)
	/// Does NOT notify players implicitly
	protected DynamicEntry Add( string proposedIndex = "" ) 
	{
		//future suggestion: support more than one kind of entries per tab (introduce EntryType field or something)
		string elementType = $"{MasterTab.Type}Entry";
		
		GameObject entryObject = Instantiate( Resources.Load<GameObject>( elementType ), transform, false );
		
		DynamicEntry dynamicEntry = entryObject.GetComponent<DynamicEntry>();

		string resultIndex = InitDynamicEntry( dynamicEntry, proposedIndex );

		RefreshPositions();
		
		if ( resultIndex != "" ) {
			Debug.Log( $"Spawning entry #[{resultIndex}]: proposed: [{proposedIndex}], entry: {dynamicEntry}" );
		} else {
			Debug.LogWarning( $"Entry \"{proposedIndex}\" spawn failure, no such entryObject {elementType}" );
		}

		if ( CustomNetworkManager.Instance._isServer /*&& notify*/ ) {
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
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

	/// Sets entry name to index, also makes its elements unique by appending index as postfix
	private string InitDynamicEntry( DynamicEntry entry, string desiredName = "" ) {
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
			externalChange = false;
		}
	}

	public override void ExecuteServer() {}
}