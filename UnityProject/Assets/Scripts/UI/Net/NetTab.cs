using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public enum NetTabType {
	None = -1,
	Vendor = 0,
	ShuttleControl = 1,
	Nuke = 2,
	Spawner = 3,
	//add your tabs here
}
/// Descriptor for unique Net UI Tab
public class NetTab : Tab {
	[HideInInspector]
	public GameObject Provider;
	public NetTabType Type = NetTabType.None;
	public NetTabDescriptor NetTabDescriptor => new NetTabDescriptor( Provider, Type );
	/// Is current tab a server tab?
	public bool IsServer => transform.parent.name == nameof(NetworkTabManager);
	
//	public static readonly NetTab Invalid = new NetworkTabInfo(null);
	private List<NetUIElement> Elements => GetComponentsInChildren<NetUIElement>(false).ToList();

	public Dictionary<string, NetUIElement> CachedElements => cachedElements;
	private Dictionary<string, NetUIElement> cachedElements = new Dictionary<string, NetUIElement>();
	
	//for server
	public HashSet<ConnectedPlayer> Peepers => peepers;
	private HashSet<ConnectedPlayer> peepers = new HashSet<ConnectedPlayer>();
	public bool IsUnobserved => Peepers.Count == 0;
	
	public ElementValue[] ElementValues => CachedElements.Values.Select( element => element.ElementValue ).ToArray(); //likely expensive

	public virtual void OnEnable() {
		InitElements();
	}

	public NetUIElement this[ string elementId ] => CachedElements.ContainsKey(elementId) ? CachedElements[elementId] : null;
	
	//for server
	public void AddPlayer( GameObject player ) {
		Peepers.Add( PlayerList.Instance.Get( player ) );
	}	
	public void RemovePlayer( GameObject player ) {
		Peepers.Remove( PlayerList.Instance.Get( player ) );
	}

	public void RescanElements() {
		InitElements();
	}

	private void InitElements() {
		var elements = Elements;
		//Init and add new elements to cache
		for ( var i = 0; i < elements.Count; i++ ) {
			NetUIElement element = elements[i];
			if ( !CachedElements.ContainsValue( element ) ) {
				element.Init();
				CachedElements.Add( element.name, element );
			}
		}

		var toRemove = new List<string>();
		//Mark non-existent elements for removal
		foreach ( var pair in CachedElements ) {
			if ( !elements.Contains(pair.Value) ) {
				toRemove.Add( pair.Key );
			}
		}
		//Remove obsolete elements from cache 
		for ( var i = 0; i < toRemove.Count; i++ ) {
			CachedElements.Remove( toRemove[i] );
		}
	}
	/// Import values.
	///
	[CanBeNull]
	public NetUIElement ImportValues( ElementValue[] values ) {
		var nonLists = new List<ElementValue>();
		bool shouldRescan = false;
		
		//set DynamicList values first (so that corresponding subelements would get created)
		for ( var i = 0; i < values.Length; i++ ) {
			var elementId = values[i].Id;
			if ( CachedElements.ContainsKey( elementId ) && this[elementId] is NetUIDynamicList ) {
				bool listContentsChanged = this[elementId].Value != values[i].Value;
				if ( listContentsChanged ) {
					this[elementId].Value = values[i].Value;
					shouldRescan = true;
				}
			} 
			else 
			{
				nonLists.Add( values[i] );
			}
		}

		//rescan elements in case of dynamic list changes
		if ( shouldRescan ) {
			RescanElements();
		}

		NetUIElement firstTouchedElement = null;
		
		//set the rest of the values 
		for ( var i = 0; i < nonLists.Count; i++ ) {
			var elementId = nonLists[i].Id;
			if ( CachedElements.ContainsKey( elementId ) ) 
			{
				var element = this[elementId];
				element.Value = nonLists[i].Value;
				
				if ( firstTouchedElement == null ) {
					firstTouchedElement = element;
				}
			} else {
				Debug.LogWarning( $"'{name}' wonky value import: can't find '{elementId}'" );
			}
		}
		return firstTouchedElement;
	}
}