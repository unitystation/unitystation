using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using NaughtyAttributes;
using Initialisation;
using Logs;
using Messages.Client;
using Objects.Electrical;
using Systems.Explosions;

namespace Systems.Hacking
{
	/// <summary>
	/// This is a controller for hacking an object. This component being attached to an object means that the object is hackable.
	/// It will check interactions with the object, and once the goal interactions have been met, it will open a hacking UI prefab.
	/// e.g. check if interacted with a screw driver, then check if
	/// </summary>
	[RequireComponent(typeof(ItemStorage))]
	public class HackingProcessBase : NetworkBehaviour, IServerDespawn, IEmpAble
	{
		private static Dictionary<Type, Dictionary<MethodInfo, Color>> ColourDictionary = new Dictionary<Type, Dictionary<MethodInfo, Color>>();

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

		private uint CableID = 1;

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
			MonoAvailableColours?.Clear();
			ColourDictionary?.Clear();
			HasRegisteredForRestart = false;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			PanelInputCurrentPorts.Clear();
			PanelOutputCurrentPorts.Clear();
			DictionaryCurrentPorts.Clear();
			Connections.Clear();
			Cables.Clear();

		}

		/// <summary>
		/// Get cleared on despawn So need to Reinitialise on spawn
		/// </summary>
		/// <param name="action"></param>
		/// <param name="FromType"></param>
		public void RegisterPort(Action action, Type FromType)
		{
			if (isServer == false) return;
			var insertCable = new Cable();

			CableID++;
			insertCable.cableCoilID = CableID;
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

			if (ColourDictionary == null)
			{
				Loggy.Log("Color dictionary wasn't found. RegisterPort has exited.", Category.Interaction);
				return;
			}

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
			PanelOutputCurrentPorts =
				PanelOutputCurrentPorts.OrderBy(item => HackingProcessBase.random.Next()).ToList();
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

		public List<Action> PulsedThisFrame = new  List<Action>();

		public Dictionary<Action, bool> RecordedState = new Dictionary<Action, bool>();
		public bool PulsePortConnectedNoLoop(Action action, bool Thisdefault = false)
		{
			if (Connections.ContainsKey(action) == false) return Thisdefault;
			if (PulsedThisFrame.Contains(action)) return RecordedState[action];

			PulsedThisFrame.Add(action);
			RecordedState[action] = Thisdefault;
			LoadManager.RegisterActionDelayed(() => PulsedThisFrame.Remove(action), 1);

			foreach (var cable in Connections[action])
			{
				cable.Impulse();
			}

			return RecordedState[action];
		}

		public bool PulsePortConnected(Action action, bool Thisdefault = false)
		{
			if (Connections.ContainsKey(action) == false) return Thisdefault;
			RecordedState[action] = Thisdefault;
			foreach (var cable in Connections[action])
			{
				cable.Impulse();
			}
			return RecordedState[action];
		}

		public void ReceivedPulse(Action action)
		{
			if (RecordedState.ContainsKey(action) == false) return;
			RecordedState[action] = !RecordedState[action];
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

		public void ProcessCustomInteraction(GameObject Player,
			RequestHackingInteraction.InteractionWith InteractionType,
			uint Referenceobject, int PanelInputID, int PanelOutputID)
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
						if (cable?.cableCoilID == Referenceobject)
						{
							Cable = cable;
							break;
						}
					}

					if (Cable == null)
					{
						Loggy.LogWarning("No cable was found for cutting", Category.Interaction);
						return;
					}

					Connections[Cable.PanelOutput].Remove(Cable);
					Cables.Remove(Cable);
					var Spawned = Spawn.ServerPrefab(OnePeaceCoil.gameObject).GameObject;


					var Hand = PlayerScript.DynamicItemStorage.GetBestHand(Spawned.GetComponent<Stackable>());
					if (Hand == null || Hand.IsOccupied)
					{
						Spawned.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(PlayerScript.ObjectPhysics.registerTile.WorldPosition);
					}
					else
					{
						Inventory.ServerAdd(Spawned, Hand);
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
						if (LocalPortInput.LocalAction == cable.PanelInput &&
							LocalPortOutput.LocalAction == cable.PanelOutput)
						{
							return; //Cable Already at position
						}
					}



					var stackable = PlayerScript.DynamicItemStorage.GetActiveHandSlot().Item.GetComponent<Stackable>();

					stackable.ServerConsume(1);

					var insertCable = new Cable();
					CableID++;
					insertCable.cableCoilID = CableID;
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

		[Button("TestRecursiveLoop")]
		public void TestRecursiveLoop()
		{
			foreach (var StartActions in Connections.Keys)
			{

				foreach (var EndActions in Connections.Keys)
				{
					if (StartActions == EndActions) continue; //Assume you don't touch the door And it's already set
					var insertCable = new Cable();
					CableID++;
					insertCable.cableCoilID = CableID;
					insertCable.PanelInput = EndActions;
					insertCable.PanelOutput = StartActions;
					Connections[StartActions].Add(insertCable);
					Cables.Add(insertCable);
				}
			}
		}

		public void OnEmp(int EmpStrength = 0)
		{
			foreach(Cable cable in Cables)
			{
				if(DMMath.Prob(50))
				{
					cable.Impulse();
				}
			}
		}

		public class Cable
		{
			public Action PanelInput;
			public Action PanelOutput;
			public uint cableCoilID;

			public void Impulse()
			{
				PanelInput.Invoke();
			}
		}
	}
}
