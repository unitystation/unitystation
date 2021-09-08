using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Initialisation;
using Messages.Client;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Objects.Electrical;
using ScriptableObjects;
using ScriptableObjects.Hacking;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// This is a controller for hacking an object. This component being attached to an object means that the object is hackable.
/// It will check interactions with the object, and once the goal interactions have been met, it will open a hacking UI prefab.
/// e.g. check if interacted with a screw driver, then check if
/// </summary>
[RequireComponent(typeof(ItemStorage))]
public class HackingProcessBase : NetworkBehaviour, IServerDespawn
{
	private static Dictionary<Type, Dictionary<MethodInfo, Color>> ColourDictionary =
		new Dictionary<Type, Dictionary<MethodInfo, Color>>();

	private static Dictionary<Type, List<Color>> MonoAvailableColours = new Dictionary<Type, List<Color>>();

	private static bool HasRegisteredForRestart = false;

	//Available colours
	//If colour blind can use the label to just say the name of the colour, Would be easy to make translation dictionary< colour, string> //TODO Colourblind stuff
	public List<Color> AvailableColours = new List<Color>()
	{
		new Color(1, 0, 0), new Color(1, 0, 1), new Color(0, 0, 1), new Color(0, 0.5f, 1), new Color(0, 1, 1),
		new Color(0, 1, 0.7f), new Color(0, 1, 0), new Color(1, 1, 0), new Color(1, 0.5f, 0), new Color(1, 1, 1),
		new Color(0.5f, 0, 0), new Color(0.5f, 0, 0.5f), new Color(0, 0, 0.5f), new Color(0, 0.5f, 0.5f),
		new Color(0, 0.5f, 0), new Color(0.5f, 0.5f, 0), new Color(0.5f, 0.25f, 0), new Color(0.5f, 0.5f, 1),
		new Color(1, 0.5f, 0.5f), new Color(1, 0.6f, 1)
	};

	public ItemTrait RemoteSignallerTrait;

	private int ID = 1;

	//The hacking GUI that is registered to this component.
	private GUI_Hacking hackingGUI;
	public GUI_Hacking HackingGUI => hackingGUI;

	[SerializeField, Required] private ItemStorage itemStorage;

	public Dictionary<Action, List<Cable>> Connections = new Dictionary<Action, List<Cable>>();

	public CableCoil OnePeaceCoil;

	public List<Cable> Cables = new List<Cable>();

	public Dictionary<Action, LocalPortData> DictionaryCurrentPorts = new Dictionary<Action, LocalPortData>();
	public List<LocalPortData> PanelOutputCurrentPorts = new List<LocalPortData>();
	public List<LocalPortData> PanelInputCurrentPorts = new List<LocalPortData>();
	//public List<RadioSignalr>  container signalrs //TODO
	//public List Grenade

	public readonly UnityEvent OnChangeServer = new UnityEvent();

	public static System.Random random = new System.Random();

	public class LocalPortData
	{
		public Action LocalAction;
		public int LocalID; //Reuse same ID for output and input
		public Color Colour;
	}

	public void AddObjectToItemStorage(GameObject gameObject)
	{
		ItemSlot SpareSlot = null;
		foreach (var TitemSlot in itemStorage.GetItemSlots())
		{
			if (TitemSlot.Item == null)
			{
				SpareSlot = TitemSlot;
				break;
			}
		}

		Inventory.ServerAdd(gameObject, SpareSlot);
	}

	public void Awake()
	{
		if (HasRegisteredForRestart == false)
		{
			HasRegisteredForRestart = true;
			if (CustomNetworkManager.IsServer)
			{
				EventManager.AddHandler(Event.RoundEnded, CleanData);
			}
		}
	}

	public static void CleanData()
	{
		MonoAvailableColours.Clear();
		ColourDictionary.Clear();
		HasRegisteredForRestart = false;
	}


	public void OnDespawnServer(DespawnInfo info)
	{
		PanelOutputCurrentPorts.Clear();
		DictionaryCurrentPorts.Clear();
		Connections.Clear();
		Cables.Clear();
		// OnChangeServerContraflow.RemoveAllListeners();//maybe?
		// hackingGUI = null;
	}

	public void RegisterPort(Action action, Type FromType)
	{
		if (isServer == false) return;
		var OnePeace = Spawn.ServerPrefab(OnePeaceCoil.gameObject);
		AddObjectToItemStorage(OnePeace.GameObject);


		var newCable = OnePeace.GameObject.GetComponent<CableCoil>();
		var insertCable = new Cable();

		insertCable.cableCoil = newCable;
		insertCable.PanelInput = action;
		insertCable.PanelOutput = action;
		if (Connections.ContainsKey(action) == false)
		{
			Connections[action] = new List<Cable>();
		}


		Connections[action].Add(insertCable);
		Cables.Add(insertCable);

		var newLocalPortData = new LocalPortData();

		newLocalPortData.LocalAction = action;
		if (ColourDictionary.ContainsKey(FromType) == false)
		{
			ColourDictionary[FromType] = new Dictionary<MethodInfo, Color>();
			MonoAvailableColours[FromType] = new List<Color>(AvailableColours);
		}

		if (ColourDictionary[FromType].ContainsKey(action.Method) == false)
		{
			ColourDictionary[FromType][action.Method] = PickARandomColourForPort(action.Method, FromType);
		}
		else
		{
			newLocalPortData.Colour = ColourDictionary[FromType][action.Method];
		}

		newLocalPortData.LocalID = ID;
		ID++;

		DictionaryCurrentPorts[action] = newLocalPortData;
		PanelOutputCurrentPorts.Add(newLocalPortData);
		PanelOutputCurrentPorts = PanelOutputCurrentPorts.OrderBy(item => HackingProcessBase.random.Next()).ToList();
		PanelInputCurrentPorts.Add(newLocalPortData);
		PanelInputCurrentPorts = PanelInputCurrentPorts.OrderBy(item => HackingProcessBase.random.Next()).ToList();
	}

	private static Color PickARandomColourForPort(MethodInfo Method, Type FromType)
	{
		int randomNumber = random.Next(0, MonoAvailableColours[FromType].Count);
		var ToReturn = MonoAvailableColours[FromType][randomNumber];
		MonoAvailableColours[FromType].RemoveAt(randomNumber);
		return ToReturn;
	}


	public void ImpulsePort(Action action)
	{
		if (Connections.ContainsKey(action) == false) return;
		foreach (var cable in Connections[action])
		{
			cable.Impulse();
		}
	}


	/// <summary>
	/// This handles placing of, cable, signaller and bomb
	/// </summary>
	/// <param name="interaction"></param>
	/// <param name="side"></param>
	/// <returns></returns>
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		//if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Cable)) return true;
		if (Validations.HasItemTrait(interaction.HandObject, RemoteSignallerTrait)) return true;
		//if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.m)) return true; //TODO Add bomb
		return false;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, RemoteSignallerTrait))
		{
			//TODO add Signaller
		}
	}

	public void ProcessCustomInteraction(GameObject Player, RequestHackingInteraction.InteractionWith InteractionType,
		GameObject Referenceobject,int PanelInputID, int PanelOutputID )
	{
		PlayerScript PlayerScript = Player.GetComponent<PlayerScript>();
		if (Validations.CanInteract(PlayerScript, NetworkSide.Server) == false) return;
		switch (InteractionType)
		{
			case RequestHackingInteraction.InteractionWith.CutWire:

				if (Validations.HasItemTrait(PlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject,
					CommonTraits.Instance.Wirecutter) == false)
				{
					return;
				}


				Cable Cable = null;
				foreach (var cable in Cables)
				{
					if (cable.cableCoil.gameObject == Referenceobject)
					{
						Cable = cable;
						break;
					}
				}

				Connections[Cable.PanelOutput].Remove(Cable);
				Cables.Remove(Cable);
				var Hand = PlayerScript.DynamicItemStorage.GetBestHand(Cable.cableCoil.GetComponent<Stackable>());
				if (Hand == null)
				{
					itemStorage.ServerTryRemove(Cable.cableCoil.gameObject, DroppedAtWorldPosition : (PlayerScript.WorldPos-this.GetComponent<RegisterTile>().WorldPositionServer));
				}
				else
				{
					itemStorage.ServerTransferGameObjectToItemSlot(Cable.cableCoil.gameObject, Hand);
				}


				OnChangeServer.Invoke();
				break;
			case RequestHackingInteraction.InteractionWith.Cable:
				//Please cable do not Spare thing

				if (Validations.HasItemTrait(PlayerScript.DynamicItemStorage.GetActiveHandSlot().ItemObject,
					CommonTraits.Instance.Cable) == false)
				{
					return;
				}

				LocalPortData LocalPortOutput = null;
				foreach (var kPortData in DictionaryCurrentPorts)
				{
					if (kPortData.Value.LocalID == PanelOutputID)
					{
						LocalPortOutput = kPortData.Value;
					}
				}


				LocalPortData LocalPortInput = null;

				foreach (var kPortData in DictionaryCurrentPorts)
				{
					if (kPortData.Value.LocalID == PanelInputID)
					{
						LocalPortInput = kPortData.Value;
					}
				}


				foreach (var cable in Cables)
				{
					if (LocalPortInput.LocalAction == cable.PanelInput && LocalPortOutput.LocalAction == cable.PanelOutput)
					{
						return; //Cable Already at position
					}
				}


				var stackable = Referenceobject.GetComponent<Stackable>();

				var OnePeace = stackable.ServerRemoveOne();
				if (OnePeace == stackable.gameObject)
				{
					ItemSlot SpareSlot = null;
					foreach (var TitemSlot in itemStorage.GetItemSlots())
					{
						if (TitemSlot.Item == null)
						{
							SpareSlot = TitemSlot;
							break;
						}
					}
					Inventory.ServerTransfer(PlayerScript.DynamicItemStorage.GetActiveHandSlot(), SpareSlot);
				}
				else
				{
					AddObjectToItemStorage(OnePeace);
				}

				var newCable = OnePeace.GetComponent<CableCoil>();
				var insertCable = new Cable();
				insertCable.cableCoil = newCable;
				Cables.Add(insertCable);

				insertCable.PanelInput = LocalPortInput.LocalAction;
				insertCable.PanelOutput = LocalPortOutput.LocalAction;

				if (Connections.ContainsKey(insertCable.PanelOutput) == false)
				{
					Connections[insertCable.PanelOutput] = new List<Cable>();
				}

				Connections[insertCable.PanelOutput].Add(insertCable);

				OnChangeServer.Invoke();
				break;
		}

	}

	public class Cable
	{
		public Action PanelInput;
		public Action PanelOutput;
		public CableCoil cableCoil;

		public void Impulse()
		{
			PanelInput.Invoke();
		}
	}
}