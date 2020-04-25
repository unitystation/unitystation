using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using YamlDotNet.Samples;

/// <summary>
/// This is a controller for hacking an object. This compoenent being attached to an object means that the object is hackable.
/// It will check interactions with the object, and once the goal interactions have been met, it will open a hacking UI prefab.
/// e.g. check if interacted with a screw driver, then check if 
/// </summary>
public abstract class HackingProcessBase : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>, IServerSpawn, IServerDespawn
{
	[SerializeField]
	[Tooltip("Whether the wires used to hack the object are initially exposed when the object is spawned.")]
	private bool wiresInitiallyExposed = false;

	[SyncVar(hook = nameof(SyncWiresExposed))]
	private bool wiresExposed = false;
	public bool WiresExposed => wiresExposed; //Public wrapper for use outside the class.

	//The hacking GUI that is registered to this component.
	private GUI_Hacking hackingGUI;
	public GUI_Hacking HackingGUI => hackingGUI;

	[SerializeField]
	[Tooltip("What the initial stage of the hack should be when the object is spawned.")]
	private int hackInitialStage = 0;
	/// <summary>
	/// This is a convenience function. Since some devices need to have several steps be completed in order to expose their wiring, this just adds a simple way of
	/// communicating between server/client what stage of the hack we're up to. Saves having to recreate it each time we make a new hacking process.
	/// </summary>
	[SyncVar(hook = nameof(SyncHackStage))]
	private int hackStage = 0;
	public int HackStage => hackStage;

	private List<HackingDevice> devices = new List<HackingDevice>();
	public List<HackingDevice> Devices => devices;

	public override void OnStartClient()
	{
		SyncWiresExposed(wiresExposed, wiresExposed);
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		SyncWiresExposed(wiresInitiallyExposed, wiresInitiallyExposed);
	}

	protected void SyncWiresExposed(bool _oldWiresExposed, bool _newWiresExposed)
	{
		wiresExposed = _newWiresExposed;
		if (_newWiresExposed)
		{
			OnWiresExposed();
		}
		else
		{
			OnWiresHidden();
		}
	}

	protected void ToggleWiresExposed()
	{
		SyncWiresExposed(wiresExposed, !wiresExposed);
	}

	protected void SyncHackStage(int _oldStage, int _newStage)
	{
		hackStage = _newStage;
		OnHackStageSet(_oldStage, _newStage);
	}

	public virtual void RegisterHackingGUI(GUI_Hacking hackUI)
	{
		hackingGUI = hackUI;
	}

	public virtual void AddNodeConnection(int keyOutput, int keyInput)
	{
		HackingNode outputNode = GetHackNodes()[keyOutput];
		HackingNode inputNode = GetHackNodes()[keyInput];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput)
		{
			outputNode.AddConnectedNode(inputNode);
		}
	}

	public virtual void RemoveNodeConnection(int keyOutput, int keyInput)
	{
		HackingNode outputNode = GetHackNodes()[keyOutput];
		HackingNode inputNode = GetHackNodes()[keyInput];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput)
		{
			outputNode.RemoveConnectedNode(inputNode);
		}
	}

	public virtual void RemoveNodeConnection(int[] connection)
	{
		Debug.Log("Attempting to remove connection.");
		if (connection.Length != 2) return;

		Debug.Log("Valid connection array, checking nodes.");
		HackingNode outputNode = GetHackNodes()[connection[0]];
		HackingNode inputNode = GetHackNodes()[connection[1]];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput)
		{
			Debug.Log("Nodes connected, removing connection.");
			outputNode.RemoveConnectedNode(inputNode);
		}
	}

	public virtual void AddNodeConnection(int[] connection)
	{
		if (connection.Length != 2) return;

		HackingNode outputNode = GetHackNodes()[connection[0]];
		HackingNode inputNode = GetHackNodes()[connection[1]];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput && !outputNode.ConnectedInputNodes.Contains(inputNode))
		{
			outputNode.AddConnectedNode(inputNode);
		}
	}

	public virtual List<int[]> GetNodeConnectionList()
	{
		List<int[]> connectionList = new List<int[]>();
		List<HackingNode> hackingNodes = GetHackNodes();
		int outputIndex = 0;
		foreach (HackingNode node in hackingNodes )
		{
			List<HackingNode> connectedNodes = node.ConnectedInputNodes;
			foreach (HackingNode connectedNode in connectedNodes)
			{
				int inputIndex = hackingNodes.IndexOf(connectedNode);
				int[] connection = { outputIndex, inputIndex };
				connectionList.Add(connection);

				Debug.Log("Output Index: " + outputIndex + " Input Index : " + inputIndex);
			}
			outputIndex++;
		}
		return connectionList;
	}

	public virtual void AddHackingDevice(HackingDevice device)
	{
		devices.Add(device);
		GetHackNodes().Add(device.InputNode);
		GetHackNodes().Add(device.OutputNode);
	}

	public virtual void RemoveHackingDevice(HackingDevice device)
	{
		devices.Remove(device);
		GetHackNodes().Remove(device.InputNode);
		GetHackNodes().Remove(device.OutputNode);
	}


	//These sounds are used when the security panel on the object is opened.
	public string openPanelSFX = null, closePanelSFX = null;

	public abstract void ClientPredictInteraction(HandApply interaction);

	public abstract void ServerPerformInteraction(HandApply interaction);

	public abstract void ServerRollbackClient(HandApply interaction);

	public abstract bool WillInteract(HandApply interaction, NetworkSide side);

	/// <summary>
	/// This function must be defined to get the hacking nodes off of the object that stores them.
	/// i.e. for a simple door, it would get the nodes from the door object.
	/// </summary>
	/// <returns></returns>
	public abstract List<HackingNode> GetHackNodes();

	public abstract void SetHackNodes(List<HackingNode> newNodes);

	/// <summary>
	/// These functions are called when the SyncVars are set using the appropriate hooks.
	/// DO NOT CALL THESE ELSEWHERE!
	/// </summary>
	protected virtual void OnWiresExposed() { }

	protected virtual void OnWiresHidden() { }

	protected virtual void OnHackStageSet(int oldStage, int newStage) { }

	public abstract void OnDespawnServer(DespawnInfo info);

	/// <summary>
	/// Check to see if a player can actually remove a connection from between two nodes. The validation of the nodes isn't done here by default, but they're included as a paramter if you wish to override this method.
	/// The connection is a 2 valued array. connection[0] is the index of the output node, connection[1] is the index of the input node.
	/// </summary>
	/// <param name="ply"></param>
	/// <param name="connection"></param>
	/// <returns></returns>
	public virtual bool ServerPlayerCanRemoveConnection(PlayerScript playerScript, int[] connection)
	{
		if (!playerScript.IsInReach(gameObject, true))
		{
			return false;
		}

		if (!WiresExposed)
		{
			return false;
		}

		Pickupable handItem = playerScript.Equipment.ItemStorage.GetActiveHandSlot().Item;
		if (handItem != null && !Validations.HasItemTrait(handItem.gameObject, CommonTraits.Instance.Wirecutter))
		{
			return true;
		}

		return true;
	}

	/// <summary>
	/// Check to see if a player can add a connection to two nodes. The validation of the nodes isn't done here by default, but they're included as a paramter if you wish to override this method.
	/// The connection is a 2 valued array. connection[0] is the index of the output node, connection[1] is the index of the input node.
	/// </summary>
	/// <param name="playerScript"></param>
	/// <param name="connection"></param>
	/// <returns></returns>
	public virtual bool ServerPlayerCanAddConnection(PlayerScript playerScript, int[] connection)
	{
		if (!playerScript.IsInReach(gameObject, true))
		{
			return false;
		}

		if (!WiresExposed)
		{
			return false;
		}

		Pickupable handItem = playerScript.Equipment.ItemStorage.GetActiveHandSlot().Item;
		if (handItem != null && !Validations.HasItemTrait(handItem.gameObject, CommonTraits.Instance.Wirecutter))
		{
			return true;
		}

		return true;
	}

}
