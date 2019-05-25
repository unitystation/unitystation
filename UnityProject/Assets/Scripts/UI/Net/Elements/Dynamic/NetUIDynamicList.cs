using System;
using System.Collections.Generic;
using System.Linq;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

/// Base class for List of dynamic entries, which can be added/removed at runtime.
/// Setting Value actually creates/removes entries for client
public class NetUIDynamicList : NetUIElement {
	public override ElementMode InteractionMode => ElementMode.ServerWrite;
	private int entryCount = 0;
	public string EntryPrefix => gameObject.name;//= String.Empty;

	public DynamicEntry[] Entries => GetComponentsInChildren<DynamicEntry>( false );

	/// <summary>
	/// Deactivate entry gameobject after putting it to pool and vice versa
	/// </summary>
	/// <typeparam name="T"></typeparam>
	protected class UniqueEntryQueue<T> : UniqueQueue<T> where T : MonoBehaviour
	{
		protected override void AfterEnqueue( T enqueuedItem )
		{
			enqueuedItem.gameObject.SetActive( false );
		}
		protected override void AfterDequeue( T dequeuedItem )
		{
			dequeuedItem.gameObject.SetActive( true );
		}
	}
	/// <summary>
	/// Pool with disabled entries, ready to be reused
	/// </summary>
	protected UniqueEntryQueue<DynamicEntry> DisabledEntryPool = new UniqueEntryQueue<DynamicEntry>();

	public GameObject EntryPrefab;

	public Dictionary<string,DynamicEntry> EntryIndex {
		get {
			var dynamicEntries = new Dictionary<string,DynamicEntry>();
			DynamicEntry[] entries = Entries;
			for ( var i = 0; i < entries.Length; i++ ) {
				DynamicEntry entry = entries[i];
				string entryName = entry.name;
				if ( dynamicEntries.ContainsKey( entryName ) ) {
					Logger.LogWarning( $"Duplicate entry name {entryName}, something's wrong", Category.NetUI );
					continue;
				}
				dynamicEntries.Add( entryName, entry );
			}

			return dynamicEntries;
		}
	}

	private void Start()
	{
		DisabledEntryPool.EnqueueAll( GetComponentsInChildren<DynamicEntry>( true ).Where( entry => !entry.gameObject.activeSelf ).ToList() );
		Logger.LogTraceFormat( "{0} dynamic list: initialized DisabledEntryPool with {1} items", Category.NetUI, gameObject.name, DisabledEntryPool.Count );
	}

	public override void Init()
	{
		if ( !EntryPrefab )
		{
			string elementType = $"{MasterTab.Type}Entry";
			Logger.LogFormat( "{0} dynamic list: EntryPrefab not assigned, trying to find it as '{1}'", Category.NetUI, gameObject.name, elementType );
			EntryPrefab = Resources.Load<GameObject>( elementType );
		}
		entryCount = 0;
		foreach ( DynamicEntry value in Entries )
		{
			InitDynamicEntry( value );
		}
	}

	public override string ToString() {
		return Value;
	}

	public virtual void Clear()
	{
		DisabledEntryPool.EnqueueAll( Entries.ToList() );

		RearrangeListItems();
	}

	/// <summary>
	/// [Server]
	/// Sets up proper layout for entries and sends coordinates to peepers
	/// </summary>
	private void RearrangeListItems()
	{
		if ( MasterTab.IsServer )
		{
			NetworkTabManager.Instance.Rescan( MasterTab.NetTabDescriptor );
			RefreshPositions();
			UpdatePeepers();
		}
	}

	/// <summary>
	/// Remove entry by its name-index
	/// </summary>
	public void Remove( string toBeRemoved ) {
		Remove(new[]{toBeRemoved});
	}

	/// <summary>
	/// Remove entries by their name-index
	/// </summary>
	public void Remove( string[] toBeRemoved )
	{
//		var mode = toBeRemoved.Length > 1 ? "Bulk" : "Single";
		var entries = EntryIndex;

		foreach ( string itemName in toBeRemoved )
		{
			var entryToRemove = entries[itemName];
//			Logger.Log( $"{mode} destroying entry #{item}({entryToRemove})" );
			DisabledEntryPool.Enqueue( entryToRemove );
		}

		RearrangeListItems();
	}

	protected DynamicEntry[] AddBulk( string[] proposedIndices )
	{
		var dynamicEntries = new DynamicEntry[proposedIndices.Length];
		var mode = proposedIndices.Length > 1 ? "Bulk" : "Single";

		for ( var i = 0; i < proposedIndices.Length; i++ ) {
			var proposedIndex = proposedIndices[i];

			DynamicEntry dynamicEntry = PoolSpawnEntry();

			string resultIndex = InitDynamicEntry( dynamicEntry, proposedIndex );


			if ( resultIndex != string.Empty )
			{
				Logger.LogTraceFormat( "{0} spawning dynamic entry #[{1}]: proposed: [{2}], entry: {3}", Category.NetUI,
					mode, resultIndex, proposedIndex, dynamicEntry );
			}
			else
			{
				Logger.LogWarning( $"Dynamic entry \"{proposedIndex}\" {mode} spawn failure, something's wrong with {dynamicEntry}", Category.NetUI );
			}

			dynamicEntries[i] = dynamicEntry;
		}

		RearrangeListItems();
		return dynamicEntries;
	}

	private DynamicEntry PoolSpawnEntry()
	{
		bool nonPool = !DisabledEntryPool.TryDequeue( out var dynamicEntry );
		if ( nonPool )
		{
			var entryObject = Instantiate( EntryPrefab, transform, false );
			dynamicEntry = entryObject.GetComponent<DynamicEntry>();
		}
		else
		{
			//Reusing
			dynamicEntry.transform.parent = transform;
		}

		return dynamicEntry;
	}

	/// Adds new entry at given index (or generates index if none is provided)
	/// Does NOT notify players implicitly
	protected DynamicEntry Add( string proposedIndex = "" )
	{
		return AddBulk(new []{proposedIndex})[0];
	}

	/// Need to run this on list change to ensure no gaps are present
	protected virtual void RefreshPositions() {
		//Adding new entries to the end by default
		var entries = Entries.OrderBy( entry => entry.name ).ToArray();
		for ( int i = 0; i < entries.Length; i++ )
		{
			SetProperPosition( entries[i], i );
		}
	}

	/// Defines the way list items are positioned.
	/// Adds next entries directly below (using height) by default
	protected virtual void SetProperPosition( DynamicEntry entry, int sortIndex = 0 ) {
		RectTransform rect = entry.gameObject.GetComponent<RectTransform>();
		rect.anchoredPosition = Vector3.down * rect.rect.height * sortIndex;
	}

	///Not just own value, include inner elements' values as well
	protected override void UpdatePeepersLogic() {
		List<ElementValue> valuesToSend = new List<ElementValue>(100) {ElementValue};
		var dynamicEntries = Entries;
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
			return string.Empty;
		}

		string index = desiredName;
		if ( desiredName == string.Empty ) {
			index = EntryPrefix == string.Empty ? entryCount++.ToString() : EntryPrefix+":"+entryCount++;
		}
		entry.name = index;

		//Making inner elements' names unique by adding "index" to the end
		foreach ( NetUIElement innerElement in entry.Elements )
		{
			if ( innerElement == entry ) {
				//not including self!
				continue;
			}

			if ( innerElement.name.Contains( DELIMITER ) )
			{
				if ( innerElement.name.Contains( DELIMITER + index ) )
				{
					//Same index - ignore
					return index;
				}
				else
				{
					Logger.LogTraceFormat( "Reuse: Inner element {0} already had indexed name, while {1} was expected", Category.NetUI, innerElement, index );
					//Different index - cut and let set it again
					innerElement.name = innerElement.name.Split( DELIMITER )[0];
				}
			}

			//postfix and not prefix because of how NetKeyButton works
			innerElement.name = innerElement.name + DELIMITER + index;
		}

		return index;
	}

	public override string Value {
		get { return string.Join( ",", EntryIndex.Keys ); }
		set {
			externalChange = true;
			var proposed = value.Split( new[]{','} , StringSplitOptions.RemoveEmptyEntries);

			if ( proposed.Length == 0 ) {
				Clear();
			} else {
				//add ones existing in proposed only, remove ones not existing in proposed
				//could probably be cheaper
				var existing = EntryIndex.Keys;
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