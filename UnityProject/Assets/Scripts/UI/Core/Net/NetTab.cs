﻿using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Messages.Server;
using UnityEngine;
using UnityEngine.Events;

public enum NetTabType
{
	None = -1,
	Vendor = 0,
	ShuttleControl = 1,
	NukeWindow = 2,
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
	SeedExtractor = 15,
	Photocopier = 16,
	ExosuitFabricator = 17,
	Autolathe = 18,
	HackingPanel = 19,
	BoozeDispenser = 20,
	SodaDispenser = 21,
	ReactorController = 22,
	BoilerTurbineController = 23,
	PipeDispenser = 24,
	DisposalBin = 25,
	PDA = 26,
	Jukebox = 27,
	Filter = 28,
	Mixer = 29,
	SpellBook = 30,
	TeleportScroll = 31,
	MagicMirror = 32,
	ContractOfApprenticeship = 33,
	ParticleAccelerator = 34,
	OreRedemptionMachine = 35,
	MaterialSilo = 36,
	SyndicateOpConsole = 37,

	//add new entres to the bottom
	// the enum name must match that of the prefab except the prefab has the word tab infront of the enum name
	// i.e TabJukeBox
}

/// Descriptor for unique Net UI Tab
public class NetTab : Tab
{
	[SerializeField]
	public ConnectedPlayerEvent OnTabClosed = new ConnectedPlayerEvent();

	/// <summary>
	/// Invoked when there is a new peeper to this tab
	/// </summary>
	[SerializeField]
	public ConnectedPlayerEvent OnTabOpened = new ConnectedPlayerEvent();

	[HideInInspector]
	public GameObject Provider;

	public RegisterTile ProviderRegisterTile;
	public NetTabType Type = NetTabType.None;
	public NetTabDescriptor NetTabDescriptor => new NetTabDescriptor(Provider, Type);

	/// Is current tab a server tab?
	public bool IsServer => transform.parent.name == nameof(NetworkTabManager);

	//	public static readonly NetTab Invalid = new NetworkTabInfo(null);
	private ISet<NetUIElementBase> Elements => new HashSet<NetUIElementBase>(GetComponentsInChildren<NetUIElementBase>(false));

	public Dictionary<string, NetUIElementBase> CachedElements { get; } = new Dictionary<string, NetUIElementBase>();

	//for server
	public HashSet<ConnectedPlayer> Peepers { get; } = new HashSet<ConnectedPlayer>();

	public bool IsUnobserved => Peepers.Count == 0;

	public ElementValue[] ElementValues => CachedElements.Values.Select(element => element.ElementValue).ToArray(); //likely expensive

	public NetUIElementBase this[string elementId] => CachedElements.ContainsKey(elementId) ? CachedElements[elementId] : null;

	public virtual void OnEnable()
	{
		if (IsServer)
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
		foreach (var element in CachedElements.Values.ToArray()) element.AfterInit();
	}

	/// <summary>
	/// Serverside-only init that happens once after fist element init
	/// </summary>
	protected virtual void InitServer() { }

	//for server
	public void AddPlayer(GameObject player)
	{
		var newPeeper = PlayerList.Instance.Get(player);
		Peepers.Add(newPeeper);
		OnTabOpened.Invoke(newPeeper);
	}

	public void RemovePlayer(GameObject player)
	{
		var newPeeper = PlayerList.Instance.Get(player);
		OnTabClosed.Invoke(newPeeper);
		Peepers.Remove(newPeeper);
	}

	public void RescanElements()
	{
		InitElements();
	}

	private void InitElements(bool serverFirstTime = false)
	{
		//Init and add new elements to cache
		var elements = Elements;
		foreach (var element in elements)
		{
			if (serverFirstTime && element is NetPageSwitcher switcher && !switcher.StartInitialized)
			{ //First time we make sure all pages are enabled in order to be scanned
				switcher.Init();
				InitElements(true);
				return;
			}

			if (CachedElements.ContainsKey(element.name)) continue;
			element.Init();

			if (CachedElements.ContainsKey(element.name))
			{
				//Someone called InitElements in Init()
				Logger.LogError($"'{name}': rescan during '{element}' Init(), aborting initial scan", Category.NetUI);
				return;
			}

			CachedElements.Add(element.name, element);
		}

		var toRemove = new List<string>();
		//Mark non-existent elements for removal
		foreach (var pair in CachedElements)
			if (!elements.Contains(pair.Value)) toRemove.Add(pair.Key);

		//Remove obsolete elements from cache
		foreach (var removed in toRemove) CachedElements.Remove(removed);
	}

	/// Import values.
	///
	[CanBeNull]
	public NetUIElementBase ImportValues(ElementValue[] values)
	{
		var nonLists = new List<ElementValue>();

		//set DynamicList values first (so that corresponding subelements would get created)
		var shouldRescan = ImportContainer(values, nonLists);

		//rescan elements in case of dynamic list changes
		if (shouldRescan) RescanElements();

		//set the rest of the values
		return ImportNonContainer(nonLists);
	}

	private bool ImportContainer(ElementValue[] values, List<ElementValue> nonLists)
	{
		var shouldRescan = false;
		foreach (var elementValue in values)
		{
			var element = this[elementValue.Id];

			if (CachedElements.ContainsKey(elementValue.Id) &&
				(element is NetUIDynamicList || element is NetPageSwitcher))
			{
				var listContentsChanged = element.ValueObject != elementValue.Value;
				if (!listContentsChanged) continue;

				element.BinaryValue = elementValue.Value;
				shouldRescan = true;
			}
			else
			{
				nonLists.Add(elementValue);
			}
		}

		return shouldRescan;
	}

	private NetUIElementBase ImportNonContainer(List<ElementValue> nonLists)
	{
		NetUIElementBase firstTouchedElement = null;
		foreach (var elementValue in nonLists)
			if (CachedElements.ContainsKey(elementValue.Id))
			{
				var element = this[elementValue.Id];
				element.BinaryValue = elementValue.Value;

				if (firstTouchedElement == null) firstTouchedElement = element;
			}
			else
			{
				Logger.LogWarning(
					$"'{name}' wonky value import: can't find '{elementValue.Id}'.\n Expected: {string.Join("/", CachedElements.Keys)}",
					Category.NetUI);
			}

		return firstTouchedElement;
	}

	/// <summary>
	/// Not sending updates and closing tab for players that don't pass the validation anymore
	/// </summary>
	public void ValidatePeepers()
	{
		foreach (var peeper in Peepers.ToArray())
		{
			bool canApply = Validations.CanApply(peeper.Script, Provider, NetworkSide.Server);

			if (!peeper.Script || !canApply)
			{
				TabUpdateMessage.Send(peeper.GameObject, Provider, Type, TabAction.Close);
			}
		}
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	public void ServerCloseTabFor(ConnectedPlayer player)
	{
		TabUpdateMessage.Send(player.GameObject, Provider, Type, TabAction.Close);
	}
}

[System.Serializable]
public class ConnectedPlayerEvent : UnityEvent<ConnectedPlayer> { }
