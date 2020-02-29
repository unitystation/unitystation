using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Events;

public enum NetTabType {
	None = -1,
	Vendor = 0,
	ShuttleControl = 1,
	Nuke = 2,
	Spawner = 3,
	Paper = 4,
	ChemistryDispenser = 5,
	Apc = 6,
	Cargo = 7,
	CloningConsole = 8,
	SecurityRecords = 9,
	Canister = 10,
	Comms = 11,
	IdConsole = 12,
	Rename = 13,
	NullRod = 14,
	//add your tabs here
}
/// Descriptor for unique Net UI Tab
public class NetTab : Tab {
	[HideInInspector]
	public GameObject Provider;
	public RegisterTile ProviderRegisterTile;
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

	/// <summary>
	/// Invoked when there is a new peeper to this tab
	/// </summary>
	public ConnectedPlayerEvent OnTabOpened = new ConnectedPlayerEvent();

	public ElementValue[] ElementValues => CachedElements.Values.Select( element => element.ElementValue ).ToArray(); //likely expensive

	public virtual void OnEnable() {
		if ( IsServer )
		{
			InitElements(true);
			InitServer();
		}
		else
		{
			InitElements();
		}
		AfterInitElements();
	}

	private void AfterInitElements()
	{
		foreach ( var element in CachedElements.Values.ToArray() )
		{
			element.AfterInit();
		}
	}

	/// <summary>
/// Serverside-only init that happens once after fist element init
/// </summary>
	protected virtual void InitServer() { }

	public NetUIElement this[ string elementId ] => CachedElements.ContainsKey(elementId) ? CachedElements[elementId] : null;

	//for server
	public void AddPlayer( GameObject player )
	{
		ConnectedPlayer newPeeper = PlayerList.Instance.Get( player );
		Peepers.Add( newPeeper );
		OnTabOpened.Invoke( newPeeper );
	}
	public void RemovePlayer( GameObject player ) {
		Peepers.Remove( PlayerList.Instance.Get( player ) );
	}

	public void RescanElements() {
		InitElements();
	}

	private void InitElements(bool serverFirstTime = false) {
		var elements = Elements;
		//Init and add new elements to cache
		foreach ( NetUIElement element in elements )
		{
			if ( serverFirstTime && element is NetPageSwitcher switcher && !switcher.StartInitialized )
			{ //First time we make sure all pages are enabled in order to be scanned
				switcher.Init();
				InitElements(true);
				return;
			}

			if ( !CachedElements.ContainsValue( element ) )
			{
				element.Init();

				if ( CachedElements.ContainsValue( element ) )
				{
					//Someone called InitElements in Init()
					Logger.LogError( $"'{name}': rescan during '{element}' Init(), aborting initial scan", Category.NetUI );
					return;
				}

				CachedElements.Add( element.name, element );
			}
		}

		var toRemove = new List<string>();
		//Mark non-existent elements for removal
		foreach ( var pair in CachedElements )
		{
			if ( !elements.Contains(pair.Value) )
			{
				toRemove.Add( pair.Key );
			}
		}

		//Remove obsolete elements from cache
		for ( var i = 0; i < toRemove.Count; i++ )
		{
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
			if ( CachedElements.ContainsKey( elementId ) &&
			     (this[elementId] is NetUIDynamicList || this[elementId] is NetPageSwitcher) ) {
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
				Logger.LogWarning( $"'{name}' wonky value import: can't find '{elementId}'.\n Expected: {string.Join("/",CachedElements.Keys)}", Category.NetUI );
			}
		}
		return firstTouchedElement;
	}

	/// <summary>
    /// Not sending updates and closing tab for players that don't pass the validation anymore
	/// </summary>
	public void ValidatePeepers()
	{
        foreach ( var peeper in Peepers.ToArray() )
        {
            bool validate = peeper.Script && Validations.CanApply(peeper.Script, Provider, NetworkSide.Server);
            if ( !validate ) {
                TabUpdateMessage.Send( peeper.GameObject, Provider, Type, TabAction.Close );
            }
        }
	}
}
public class ConnectedPlayerEvent : UnityEvent<ConnectedPlayer> { }