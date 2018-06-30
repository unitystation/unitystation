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

	public DynamicEntry[] EntryArray => GetComponentsInChildren<DynamicEntry>( false );

	public Dictionary<string,DynamicEntry> Entries {
		get {
			var dynamicEntries = new Dictionary<string,DynamicEntry>();
			DynamicEntry[] entries = EntryArray;
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
		var entryArray = EntryArray;
		for ( var i = 0; i < entryArray.Length; i++ ) {
			var value = entryArray[i];
			InitDynamicEntry( value );
		}
	}

	public override string ToString() {
		return Value;
	}

	public void Clear() {
		var entryArray = EntryArray;
		for ( var i = 0; i < entryArray.Length; i++ ) {
			var entry = entryArray[i];
			entry.gameObject.SetActive( false );
		}

		if ( MasterTab.IsServer ) {
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
			UpdatePeepers();
		}
		RefreshPositions();
	}

	protected void Remove( string toBeRemoved ) {
		Remove(new[]{toBeRemoved});
	}
	protected void Remove( string[] toBeRemoved )
	{ 
		var mode = toBeRemoved.Length > 1 ? "Bulk" : "Single";
		var entries = Entries;
		
		for ( var i = 0; i < toBeRemoved.Length; i++ ) {
			var item = toBeRemoved[i];
			var entryToRemove = entries[item];
//			Debug.Log( $"{mode} destroying entry #{item}({entryToRemove})" );
			entryToRemove.gameObject.SetActive( false );
		}

		if ( MasterTab.IsServer ) {
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
			UpdatePeepers();
		}
		RefreshPositions();
	}

	protected DynamicEntry[] AddBulk( string[] proposedIndices ) 
	{
		var dynamicEntries = new DynamicEntry[proposedIndices.Length];
		var mode = proposedIndices.Length > 1 ? "Bulk" : "Single";
		
		string elementType = $"{MasterTab.Type}Entry";
		GameObject prefab = Resources.Load<GameObject>( elementType );

		for ( var i = 0; i < proposedIndices.Length; i++ ) {
			var proposedIndex = proposedIndices[i];
			//future suggestion: support more than one kind of entries per tab (introduce EntryType field or something)

			GameObject entryObject = Instantiate( prefab, transform, false );

			DynamicEntry dynamicEntry = entryObject.GetComponent<DynamicEntry>();

			string resultIndex = InitDynamicEntry( dynamicEntry, proposedIndex );

			RefreshPositions();

			if ( resultIndex != "" ) {
//				Debug.Log( $"{mode} spawning entry #[{resultIndex}]: proposed: [{proposedIndex}], entry: {dynamicEntry}" );
			} else {
				Debug.LogWarning( $"Entry \"{proposedIndex}\" {mode} spawn failure, no such entryObject {elementType}" );
			}

			dynamicEntries[i] = dynamicEntry;
		}

		if ( MasterTab.IsServer ) {
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
		}
		return dynamicEntries;
	}

	/// Adds new entry at given index (or generates index if none is provided)
	/// Does NOT notify players implicitly
	protected DynamicEntry Add( string proposedIndex = "" ) 
	{
		return AddBulk(new []{proposedIndex})[0];
	}

	/// Need to run this on list change to ensure no gaps are present
	protected virtual void RefreshPositions() {
		var entries = EntryArray;
		for ( var i = 0; i < entries.Length; i++ ) {
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
		List<ElementValue> valuesToSend = new List<ElementValue>(100) {ElementValue};
		var dynamicEntries = EntryArray;
		for ( var i = 0; i < dynamicEntries.Length; i++ ) 
		{
			var entry = dynamicEntries[i];
			var entryElements = entry.Elements;
			
			for ( var j = 0; j < entryElements.Length; j++ ) 
			{
				var element = entryElements[j];
				valuesToSend.Add( element.ElementValue );
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
		for ( var i = 0; i < entry.Elements.Length; i++ ) {
			if ( entry.Elements[i] == entry ) {
				//not including self!
				continue;
			}
			//postfix and not prefix because of how NetKeyButton works
			entry.Elements[i].name = entry.Elements[i].name + "_" + index; 
		}

		return index;
	}

	public override string Value {
		get { return string.Join( ",", Entries.Keys ); }
		set {
			externalChange = true;
			var proposed = value.Split( new[]{','} , StringSplitOptions.RemoveEmptyEntries);

			if ( proposed.Length == 0 ) {
				Clear();
			} else {
				//add ones existing in proposed only, remove ones not existing in proposed
				//could probably be cheaper
				var existing = Entries.Keys;
				var toRemove = existing.Except( proposed ).ToArray();
				var toAdd = proposed.Except( existing ).ToArray();
				
				Remove( toRemove );

				AddBulk( toAdd );
			}
			externalChange = false;
		}
	}

	public override void ExecuteServer() {}
}