using System;
using System.Collections.Generic;
using System.Linq;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// For server.
/// </summary>
public class NetworkTabManager : MonoBehaviour {
	//Declare in awake as NetTabManager needs to be destroyed on each scene change
	public static NetworkTabManager Instance;
	private readonly Dictionary<NetworkTab, NetworkTabInfo> openTabs = 
		new Dictionary<NetworkTab, NetworkTabInfo>();
	public List<ConnectedPlayer> GetPeepers(GameObject provider, TabType type) {
		var info = openTabs[Tab( provider, type )]; //unsafe
		if ( info.IsUnobserved ) {
			return new List<ConnectedPlayer>();
		}
		return info.Peepers.ToList();
	}

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	/// Used when a new dynamic element is added/removed
	public void ReInit( NetworkTab tab ) {
		Get( tab ).RescanElements();
	}


	///Create new NetworkTabInfo if it doesn't exist, otherwise add player to it
	public void Add( NetworkTab tab, GameObject player ) {
		if ( !openTabs.ContainsKey( tab ) ) {
			//Spawning new one
			openTabs.Add( tab, tab.Spawn() );
		} 
		openTabs[tab].AddPlayer( player );
	}
	public void Add( GameObject provider, TabType type, GameObject player ) {
		Add( Tab(provider, type), player );
	}

	public void Remove( GameObject provider, TabType type, GameObject player ) {
		Remove( Tab( provider, type ), player );
	}

	/// remove player from NetworkTabInfo, keeping the tab
	public void Remove( NetworkTab tab, GameObject player ) {
		NetworkTabInfo t = openTabs[tab];
		t.RemovePlayer( player );
	}

	public NetworkTabInfo Get( GameObject provider, TabType type ) {
		return Get( Tab(provider, type) );
	}

	public NetworkTabInfo Get( NetworkTab tab ) {
		return openTabs.ContainsKey(tab) ? openTabs[tab] : NetworkTabInfo.Invalid;
	}

	private static NetworkTab Tab( GameObject provider, TabType type ) {
		return new NetworkTab( provider, type );
	}
}

public struct NetworkTab {
	private readonly NetworkTabTrigger provider;
	private readonly TabType type;

	public NetworkTab( GameObject provider, TabType type ) {
		this.provider = provider.GetComponent<NetworkTabTrigger>();
		this.type = type;

	}

	public NetworkTabInfo Spawn() {
		var tabObject = Object.Instantiate( Resources.Load( $"Tab{type}" ) as GameObject );
		tabObject.GetComponent<NetUITab>().Provider = provider.gameObject;
		var tab = tabObject;
		return new NetworkTabInfo(tab);
	}
}

public class NetworkTabInfo
{
	public static readonly NetworkTabInfo Invalid = new NetworkTabInfo(null);
	private readonly GameObject reference;
	private List<NetUIElement> Elements => reference.GetComponentsInChildren<NetUIElement>(true).ToList();

	public Dictionary<string, NetUIElement> CachedElements { get; }
	public HashSet<ConnectedPlayer> Peepers { get; }
	/// Actual tab gameobject
	public GameObject Reference => reference;
	public bool IsUnobserved => Peepers.Count == 0;
	public ElementValue[] ElementValues => CachedElements.Values.Select( element => element.ElementValue ).ToArray(); //likely expensive

	public NetworkTabInfo( GameObject reference ) {
		this.reference = reference;
		Peepers = new HashSet<ConnectedPlayer>();
		CachedElements = new Dictionary<string, NetUIElement>();
		if ( reference != null ) {			
			InitElements();
		}
	}
	
	public NetUIElement this[ string elementId ] => CachedElements.ContainsKey(elementId) ? CachedElements[elementId] : null;
	
	public void AddPlayer( GameObject player ) {
		Peepers.Add( PlayerList.Instance.Get( player ) );
	}	
	public void RemovePlayer( GameObject player ) {
		Peepers.Remove( PlayerList.Instance.Get( player ) );
	}

	public void RescanElements() {
//		CachedElements.Clear();
		InitElements();
	}

	private void InitElements() {
		var elements = Elements;
		for ( var i = 0; i < elements.Count; i++ ) {
			NetUIElement element = elements[i];
			if ( !CachedElements.ContainsValue( element ) ) {
				element.Init();
				CachedElements.Add( element.name, element );
			}
		}
		foreach ( var pair in CachedElements ) {
			if ( !elements.Contains(pair.Value) ) {
				CachedElements.Remove( pair.Key );
			}
		}
	}
	//import values
	public void ImportValues( ElementValue[] values ) {
		for ( var i = 0; i < values.Length; i++ ) {
			var elementId = values[i].Id;
			if ( CachedElements.ContainsKey( elementId ) ) {
				this[elementId].Value = values[i].Value;//FIXME: create entries first, then set values!
			} else {
				Debug.LogWarning( $"'{reference.name}' wonky value import: can't find '{elementId}'" );
			}
		}
	}
}

public struct TabElement {
	public string Id;
	public int Value;
}