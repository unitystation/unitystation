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
	public Dictionary<string, NetUIElement> Elements { get; }
	public HashSet<ConnectedPlayer> Peepers { get; }
	/// Actual tab gameobject
	public GameObject Reference => reference;
	public bool IsUnobserved => Peepers.Count == 0;
	public ElementValue[] ElementValues => Elements.Values.Select( element => element.ElementValue ).ToArray(); //likely expensive

	public NetworkTabInfo( GameObject reference ) {
		this.reference = reference;
		Peepers = new HashSet<ConnectedPlayer>();
		Elements = new Dictionary<string, NetUIElement>();
		if ( reference != null ) {			
			InitElements();
		}
	}
	
	public NetUIElement this[ string elementId ] => Elements.ContainsKey(elementId) ? Elements[elementId] : null;
	
	public void AddPlayer( GameObject player ) {
		Peepers.Add( PlayerList.Instance.Get( player ) );
	}	
	public void RemovePlayer( GameObject player ) {
		Peepers.Remove( PlayerList.Instance.Get( player ) );
	}

	private void InitElements() {
		foreach ( NetUIElement element in reference.GetComponentsInChildren<NetUIElement>(true) ) {
			Elements.Add( element.name, element );
		}
	}
	//import values
	public void ImportValues( ElementValue[] values ) {
		for ( var i = 0; i < values.Length; i++ ) {
			var elementId = values[i].Id;
			if ( Elements.ContainsKey( elementId ) ) {
				this[elementId].Value = values[i].Value;
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