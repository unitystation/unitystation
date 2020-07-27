using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using YamlDotNet.Samples;
using UnityEngine.Events;

/// <summary>
/// This is a controller for hacking an object. This compoenent being attached to an object means that the object is hackable.
/// It will check interactions with the object, and once the goal interactions have been met, it will open a hacking UI prefab.
/// e.g. check if interacted with a screw driver, then check if
/// </summary>
[RequireComponent(typeof(ItemStorage))]
public abstract class HackingProcessBase : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>, IServerDespawn
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

	// Commented out because it is unused:
	// [SerializeField]
	// [Tooltip("What the initial stage of the hack should be when the object is spawned.")]
	// private int hackInitialStage = 0;

	/// <summary>
	/// This is a convenience function. Since some devices need to have several steps be completed in order to expose their wiring, this just adds a simple way of
	/// communicating between server/client what stage of the hack we're up to. Saves having to recreate it each time we make a new hacking process.
	/// </summary>
	[SyncVar(hook = nameof(SyncHackStage))]
	private int hackStage = 0;
	public int HackStage => hackStage;

	private List<HackingDevice> devices = new List<HackingDevice>();
	public List<HackingDevice> Devices => devices;

	private ItemStorage itemStorage;
	public ItemStorage ItemStorage => itemStorage;

	public HackingNodeList nodeInfo;

	/// <summary>
	/// Contains all the hacking nodes associated with this object.
	/// </summary>
	protected List<HackingNode> hackNodes = new List<HackingNode>();

	void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
	}

	public override void OnStartClient()
	{
		itemStorage = GetComponent<ItemStorage>();
		if (isClientOnly)
		{
			ClientGenerateNodesFromNodeInfo();
		}
		SyncWiresExposed(wiresExposed, wiresExposed);
	}

	public override void OnStartServer()
	{
		itemStorage = GetComponent<ItemStorage>();
		ServerGenerateNodesFromNodeInfo();
		ServerLinkHackingNodes();
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


	public abstract void ServerLinkHackingNodes();

	public virtual void ServerGenerateNodesFromNodeInfo()
	{
		foreach (HackingNodeInfo inf in nodeInfo.nodeInfoList)
		{
			HackingNode newNode = new HackingNode();
			newNode.IsInput = inf.IsInput;
			newNode.IsOutput = inf.IsOutput;
			newNode.IsDeviceNode = inf.IsDeviceNode;
			newNode.InternalIdentifier = inf.InternalIdentifier;
			newNode.HiddenLabel = inf.HiddenLabel;
			newNode.PublicLabel = inf.PublicLabel;

			hackNodes.Add(newNode);
		}
	}

	//Node list is just undefined nodes for the client. Important, because it means that the client does not know what nodes does what. It just needs the same amount of nodes.
	public virtual void ClientGenerateNodesFromNodeInfo()
	{
		foreach (HackingNodeInfo inf in nodeInfo.nodeInfoList)
		{
			HackingNode newNode = new HackingNode();
			newNode.IsInput = inf.IsInput;
			newNode.IsOutput = inf.IsOutput;
			newNode.IsDeviceNode = inf.IsDeviceNode;
			newNode.PublicLabel = inf.PublicLabel;

			hackNodes.Add(newNode);
		}
	}

	public HackingNode GetNodeWithInternalIdentifier(HackingIdentifier identifier)
	{
		return hackNodes.Find(x => x.InternalIdentifier == identifier);
	}

	public void SendOutputToConnectedNodes(HackingIdentifier identifier, GameObject originator = null)
	{
		HackingNode node = GetNodeWithInternalIdentifier(identifier);
		node.SendOutputToConnectedNodes(originator);
	}


	/// <summary>
	/// Add a connection between two nodes in the hacking device. keyOutput is the index of the output node, similar for key input.
	/// </summary>
	/// <param name="keyOutput"></param>
	/// <param name="keyInput"></param>
	public virtual void AddNodeConnection(int keyOutput, int keyInput)
	{
		HackingNode outputNode = GetHackNodes()[keyOutput];
		HackingNode inputNode = GetHackNodes()[keyInput];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput)
		{
			outputNode.AddConnectedNode(inputNode);
		}
	}

	/// <summary>
	/// Remove a connection between two nodes. keyOutput is the index of the output node, similar for key input.
	/// </summary>
	/// <param name="keyOutput"></param>
	/// <param name="keyInput"></param>
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
		if (connection.Length != 2) return;

		HackingNode outputNode = GetHackNodes()[connection[0]];
		HackingNode inputNode = GetHackNodes()[connection[1]];

		if (outputNode != null && inputNode != null && outputNode.IsOutput && inputNode.IsInput)
		{
			outputNode.RemoveConnectedNode(inputNode);
		}
	}

	public virtual void AddNodeConnection(int[] connection)
	{
		if (connection.Length != 2) return;

		if(hackNodes.ElementAtOrDefault(connection[0]) == null || hackNodes[connection[0]] == null) return;

		HackingNode outputNode = hackNodes[connection[0]];

		if (hackNodes.ElementAtOrDefault(connection[1]) == null || hackNodes[connection[1]] == null) return;

		HackingNode inputNode = hackNodes[connection[1]];

		bool nodeNotNull = outputNode != null && inputNode != null;

		bool isOutputAndInput = outputNode.IsOutput && inputNode.IsInput;

		bool notAlreadyHasNode = !outputNode.ConnectedInputNodes.Contains(inputNode);

		if (nodeNotNull && isOutputAndInput && notAlreadyHasNode)
		{
			outputNode.AddConnectedNode(inputNode);
		}
	}

	/// <summary>
	/// Get the list of connetions between nodes as a list of integer arrays. The first integer in each array is the output nodes index, and the second integer is the input nodes index.
	/// </summary>
	/// <returns></returns>
	public virtual List<int[]> GetNodeConnectionList()
	{
		List<int[]> connectionList = new List<int[]>();
		int outputIndex = 0;
		foreach (HackingNode node in hackNodes)
		{
			List<HackingNode> connectedNodes = node.ConnectedInputNodes;
			foreach (HackingNode connectedNode in connectedNodes)
			{
				int inputIndex = hackNodes.IndexOf(connectedNode);
				int[] connection = { outputIndex, inputIndex };
				connectionList.Add(connection);
			}
			outputIndex++;
		}
		return connectionList;
	}

	/// <summary>
	/// Adds a hacking device to the panel. Usually called in conjunction with ServerStoreHackingDevice if called serverside. Can be called clientside, but will only modify client side devices.
	/// </summary>
	/// <param name="device"></param>
	public virtual void AddHackingDevice(HackingDevice device)
	{
		devices.Add(device);
		hackNodes.Add(device.InputNode);
		hackNodes.Add(device.OutputNode);
	}

	/// <summary>
	/// Removes a hacking device from the panel. Usually called in conjunction with ServerPlayerRemoveHackingDevice if called serverside. Can be called clientside, but will only modify clientside devices.
	/// </summary>
	/// <param name="device"></param>
	public virtual void RemoveHackingDevice(HackingDevice device)
	{
		devices.Remove(device);
		hackNodes.Remove(device.InputNode);

		//Ensure that nothing is connected to the device when it's removed.
		hackNodes.ForEach(x => x.RemoveConnectedNode(device.InputNode));

		device.OutputNode.RemoveAllConnectedNodes();
		hackNodes.Remove(device.OutputNode);
	}

	/// <summary>
	/// Removes all devices. Does not remove them from internal storage. If this is called without removing them from storage, they'll be stuck there.
	/// </summary>
	public virtual void RemoveAllDevices()
	{
		foreach (HackingDevice device in devices.ToList())
		{
			RemoveHackingDevice(device);
		}
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
	public virtual List<HackingNode> GetHackNodes()
	{
		return hackNodes;
	}

	/// <summary>
	/// These functions are called when the SyncVars are set using the appropriate hooks.
	/// DO NOT CALL THESE ELSEWHERE!
	/// Used to support things happening when wires are exposed.
	/// </summary>
	protected virtual void OnWiresExposed() { }

	protected virtual void OnWiresHidden() { }

	/// <summary>
	/// This is called in the appropraite SyncVar hooks. Used to make stuff happen when progress is made on hacking the object.
	/// Could update sprites, play sounds, etc.
	/// </summary>
	/// <param name="oldStage"></param>
	/// <param name="newStage"></param>
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
		if (handItem == null || !Validations.HasItemTrait(handItem.gameObject, CommonTraits.Instance.Wirecutter))
		{
			return false;
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

		return true;
	}

	public virtual void ServerPlayerRemoveConnection(PlayerScript player, int[] connection)
	{
		int outIndex = connection[0];
		int inIndex = connection[1];

		HackingNode node = hackNodes[outIndex];
		if (node != null)
		{
			node.WireCutCallback(player.gameObject);
		}
		RemoveNodeConnection(connection);
	}

	/// <summary>
	/// Check to see if a player can add a device to the panel. By default, only checks if they're in reach and the wires are exposed.
	/// </summary>
	/// <param name="playerScript"></param>
	/// <param name="device"></param>
	/// <returns></returns>
	public virtual bool ServerPlayerCanAddDevice(PlayerScript playerScript, HackingDevice device)
	{
		if (!playerScript.IsInReach(gameObject, true))
		{
			return false;
		}

		if (!WiresExposed)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Checks to see if a player can remove a device from the panel. By default, only checks if they're in each and the wires are exposed.
	/// </summary>
	/// <param name="playerScript"></param>
	/// <param name="device"></param>
	/// <returns></returns>
	public virtual bool ServerPlayerCanRemoveDevice(PlayerScript playerScript, HackingDevice device)
	{
		if (!playerScript.IsInReach(gameObject, true))
		{
			return false;
		}

		if (!WiresExposed)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Called when we want to store a device in the hacking panel.
	/// </summary>
	/// <param name="hackDevice"></param>
	public virtual void ServerStoreHackingDevice(HackingDevice hackDevice)
	{
		Pickupable item = hackDevice.GetComponent<Pickupable>();
		if (item.ItemSlot != null)
		{
			Inventory.ServerPerform(InventoryMove.Transfer(item.ItemSlot, itemStorage.GetBestSlotFor(item)));
		}
		else
		{
			Inventory.ServerPerform(InventoryMove.Add(item, itemStorage.GetBestSlotFor(item)));
		}
	}

	/// <summary>
	/// Called when a player retrieves a hacking device from the panel.
	/// </summary>
	/// <param name="playerScript"></param>
	/// <param name="hackDevice"></param>
	public virtual void ServerPlayerRemoveHackingDevice(PlayerScript playerScript, HackingDevice hackDevice)
	{
		Pickupable item = hackDevice.GetComponent<Pickupable>();
		ItemSlot handSlot = playerScript.Equipment.ItemStorage.GetActiveHandSlot();
		Pickupable handItem = handSlot.Item;
		if (handItem == null)
		{
			Inventory.ServerPerform(InventoryMove.Transfer(item.ItemSlot, handSlot));
		}
	}

}

public enum HackingIdentifier
{
	Unset,
	OnShouldOpen,
	OpenDoor,
	OnShouldClose,
	CloseDoor,
	OnIdRejected,
	RejectId,
	OnIdAccepted,
	AcceptId,
	ShouldDoPressureWarning,
	DoPressureWarning,
	PowerOut,
	PowerIn,
	DummyOut,
	DummyIn,
	OutsideSignalOpen,
	OutsideSignalClose,
	CancelCloseTimer
}