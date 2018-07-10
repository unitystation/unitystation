using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UI;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// For server.
/// </summary>
public class NetworkTabManager : MonoBehaviour {
	//Declare in awake as NetTabManager needs to be destroyed on each scene change
	public static NetworkTabManager Instance;
	private readonly Dictionary<NetTabDescriptor, NetTab> openTabs = 
		new Dictionary<NetTabDescriptor, NetTab>();
	public List<ConnectedPlayer> GetPeepers(GameObject provider, NetTabType type) {
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
	public void Rescan( NetTabDescriptor tabDescriptor ) {
		Get( tabDescriptor ).RescanElements();
	}


	///Create new NetworkTabInfo if it doesn't exist, otherwise add player to it
	public void Add( NetTabDescriptor tabDescriptor, GameObject player ) {
		if ( !openTabs.ContainsKey( tabDescriptor ) ) {
			//Spawning new one
			openTabs.Add( tabDescriptor, tabDescriptor.Spawn() );
		} 
		openTabs[tabDescriptor].AddPlayer( player );
	}
	public void Add( GameObject provider, NetTabType type, GameObject player ) {
		Add( Tab(provider, type), player );
	}

	public void Remove( GameObject provider, NetTabType type, GameObject player ) {
		Remove( Tab( provider, type ), player );
	}

	/// remove player from NetworkTabInfo, keeping the tab
	public void Remove( NetTabDescriptor tabDescriptor, GameObject player ) {
		NetTab t = openTabs[tabDescriptor];
		t.RemovePlayer( player );
	}

	public NetTab Get( GameObject provider, NetTabType type ) {
		return Get( Tab(provider, type) );
	}

	public NetTab Get( NetTabDescriptor tabDescriptor ) {
		return openTabs.ContainsKey( tabDescriptor ) ? openTabs[tabDescriptor] : null; //NetworkTabInfo.Invalid;
	}

	private static NetTabDescriptor Tab( GameObject provider, NetTabType type ) {
		return new NetTabDescriptor( provider, type );
	}
}

public struct NetTabDescriptor {
	private readonly NetworkTabTrigger provider;
	private readonly NetTabType type;

	public NetTabDescriptor( GameObject provider, NetTabType type ) {
		this.provider = provider?.GetComponent<NetworkTabTrigger>();
		this.type = type;
		if ( type == NetTabType.None ) {
			Debug.LogError( "You forgot to set a proper NetTabType in your new tab!\n" +
			                "Go to Prefabs/GUI/Resources and see if any prefabs starting with Tab has Type=None" );
		}
	}

	public NetTab Spawn() {
		var tabObject = Object.Instantiate( Resources.Load( $"Tab{type}" ) as GameObject );
		NetTab netTab = tabObject.GetComponent<NetTab>();
		netTab.Provider = provider.gameObject;
		return netTab;
	}
}