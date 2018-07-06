using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// For server.
/// </summary>
public class NetworkTabManager : MonoBehaviour {
	//Declare in awake as NetTabManager needs to be destroyed on each scene change
	public static NetworkTabManager Instance;
	private readonly Dictionary<NetTabDescriptor, NetTab> openTabs = new Dictionary<NetTabDescriptor, NetTab>();
	
	public List<ConnectedPlayer> GetPeepers(GameObject provider, NetTabType type) 
	{
		var descriptor = Tab( provider, type );
		if ( !openTabs.ContainsKey( descriptor ) ) {
			return new List<ConnectedPlayer>();
		}
		var info = openTabs[descriptor];
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
			// Clearing net tabs on round restart
//			Debug.LogError( "NTM cleanup!" );
			foreach ( var tab in Instance.openTabs ) {
				Destroy(tab.Value.gameObject);
			}
			Instance.openTabs.Clear();
		}
	}

	/// Used when a new dynamic element is added/removed
	public void Rescan( NetTabDescriptor tabDescriptor ) {
		Get( tabDescriptor ).RescanElements();
	}

	///Create new NetworkTabInfo if it doesn't exist, otherwise add player to it
	public void Add( NetTabDescriptor tabDescriptor, GameObject player ) 
	{
		if ( tabDescriptor.Equals( NetTabDescriptor.Invalid ) ) {
			return;
		}
		
		if ( !openTabs.ContainsKey( tabDescriptor ) ) {
			//Spawning new one
			openTabs.Add( tabDescriptor, tabDescriptor.Spawn(transform) );
		}
		NetTab tab = openTabs[tabDescriptor];
//		tab.gameObject.SetActive( true );
		tab.AddPlayer( player );
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
//		if ( t.Peepers.Count == 0 ) {
//			t.gameObject.SetActive( false );
//		}
	}

	public NetTab Get( GameObject provider, NetTabType type ) {
		return Get( Tab(provider, type) );
	}

	public NetTab Get( NetTabDescriptor tabDescriptor ) {
		return openTabs.ContainsKey( tabDescriptor ) ? openTabs[tabDescriptor] : null; //NetworkTabInfo.Invalid;
	}

	private static NetTabDescriptor Tab( GameObject provider, NetTabType type ) {
		return provider == null ? NetTabDescriptor.Invalid : new NetTabDescriptor( provider, type );
	}
}

public struct NetTabDescriptor {
	public static readonly NetTabDescriptor Invalid = new NetTabDescriptor(null, NetTabType.None);
	private readonly NetworkTabTrigger provider;
	private readonly NetTabType type;

	public NetTabDescriptor( GameObject provider, NetTabType type ) {
		this.provider = provider != null ? provider.GetComponent<NetworkTabTrigger>() : null;
		this.type = type;
		if ( type == NetTabType.None && this.provider != null ) {
			Debug.LogError( "You forgot to set a proper NetTabType in your new tab!\n" +
			                "Go to Prefabs/GUI/Resources and see if any prefabs starting with Tab has Type=None" );
		}
	}

	public NetTab Spawn(Transform parent) {
		if ( provider == null ) {
			return null;
		}
		var tabObject = Object.Instantiate( Resources.Load( $"Tab{type}" ) as GameObject, parent );
		NetTab netTab = tabObject.GetComponent<NetTab>();
		netTab.Provider = provider.gameObject;
		return netTab;
	}
}