using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Hacking : NetTab
{
	private IHackable hackInterface;
	private HackingProcessBase hackProcess;
	public HackingProcessBase HackProcess => hackProcess;

	/// <summary>
	/// This is a list of all the hacking nodes for this object.
	/// </summary>
	private List<HackingNode> hackNodes;
	public List<HackingNode> HackNodes => hackNodes;

	/// <summary>
	/// Input and output node lists, comprised of nodes from the list of all hacking nodes.
	/// </summary>
	private List<HackingNode> inputNodes;
	public List<HackingNode> InputNodes => inputNodes;

	private List<HackingNode> outputNodes;
	public List<HackingNode> OutputNodes => outputNodes;

	/// <summary>
	/// The list of node UI objects the panel has created. This will be used to delete/modify existing nodes if they get updated/replaced.
	/// </summary>
	private List<GUI_HackingNode> nodeUIObjects = new List<GUI_HackingNode>();
	public List<GUI_HackingNode> NodeUIObjects => nodeUIObjects;

	/// <summary>
	/// List of output nodes and input nodes.
	/// </summary>
	private List<GUI_HackingNode> inputNodeUIObjects = new List<GUI_HackingNode>();
	public List<GUI_HackingNode> InputNodeUIObjects => inputNodeUIObjects;

	private List<GUI_HackingNode> outputNodeUIObjects = new List<GUI_HackingNode>();
	public List<GUI_HackingNode> OutputNodeUIObjects => outputNodeUIObjects;

	/// <summary>
	/// List of wires UI objects.
	/// </summary>
	private List<GUI_HackingWire> hackingWires = new List<GUI_HackingWire>();
	public List<GUI_HackingWire> HackingWires => hackingWires;


	[SerializeField]
	private RectTransform inputsLayout;

	[SerializeField]
	private RectTransform outputsLayout;

	[SerializeField]
	private GameObject inputHackingNodeUIPrefab;

	[SerializeField]
	private GameObject outputHackingNodeUIPrefab;

	[SerializeField]
	private GameObject connectingWireUIPrefab;

	[SerializeField]
	[Tooltip("This is the cell size of the hacking nodes when displayed by the UI. The cell size isn't calculated dynamically, but the spacing betweem them is.")]
	private Vector2 nodeCellSize;

	private bool isAddingWire = false;
	public bool IsAddingWire => isAddingWire;


	/// <summary>
	/// These are used interanlly for when a new wire is being added.
	/// </summary>
	private GUI_HackingNode newWireOutput;
	private GUI_HackingNode newWireInput;

	private List<int[]> connectionList = new List<int[]>();

	void Start()
    {
		if (Provider != null)
		{
			hackInterface = Provider.GetComponentInChildren<IHackable>();
			hackProcess = Provider.GetComponentInChildren<HackingProcessBase>();
			hackProcess.RegisterHackingGUI(this);

			SetNodeList(hackProcess.GetHackNodes());
			connectionList = hackProcess.GetNodeConnectionList();

			if (!IsServer)
			{
				RequestHackingNodeConnections.Send(PlayerManager.LocalPlayerScript.gameObject, hackProcess.gameObject);
			}
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (!IsServer && hackProcess != null)
		{
			RequestHackingNodeConnections.Send(PlayerManager.LocalPlayerScript.gameObject, hackProcess.gameObject);
		}
	}

	/// <summary>
	/// Set the list of nodes this component will use. Importantly, this should be sent this information from the server. The client doesn't actually need to know what nodes are attached where.
	/// At least until it opens the UI.
	/// </summary>
	/// <param name="nodeList"></param>
	public void SetNodeList(List<HackingNode> nodeList)
	{
		DeleteOldNodes();
		hackNodes = nodeList;
		GenerateNodeUI();
	}

	private void DeleteOldNodes()
	{
		foreach(GUI_HackingNode UINode in nodeUIObjects)
		{
			Destroy(UINode.gameObject);
		}
		nodeUIObjects = new List<GUI_HackingNode>();
		inputNodeUIObjects = new List<GUI_HackingNode>();
		outputNodeUIObjects = new List<GUI_HackingNode>();

		hackNodes = new List<HackingNode>();
		inputNodes = new List<HackingNode>();
		outputNodes = new List<HackingNode>();
	}

	/// <summary>
	/// Generate the UI to represent the hacking nodes. This will be input nodes 
	/// </summary>
	private void GenerateNodeUI()
	{
		foreach (HackingNode node in hackNodes)
		{
			if (node.IsInput)
			{
				inputNodes.Add(node);
			}
			else
			{
				outputNodes.Add(node);
			}
		}
		GenerateInputNodeUI();
		GenerateOutputNodeUI();

		HorizontalLayoutGroup inputLayout = inputsLayout.GetComponentInChildren<HorizontalLayoutGroup>();
		inputLayout.CalculateLayoutInputHorizontal();
		inputLayout.CalculateLayoutInputVertical();
		inputLayout.SetLayoutHorizontal();
		inputLayout.SetLayoutVertical();

		HorizontalLayoutGroup outputLayout = outputsLayout.GetComponentInChildren<HorizontalLayoutGroup>();
		outputLayout.CalculateLayoutInputHorizontal();
		outputLayout.CalculateLayoutInputVertical();
		outputLayout.SetLayoutHorizontal();
		outputLayout.SetLayoutVertical();

		GenerateNodeConnections();
	}
	/// <summary>
	/// Gets the UI component of a node inside the system. Every node should have a UI component, so this shouldn't ever return null. If it does, uh oh.
	/// </summary>
	/// <param name="node"></param>
	/// <returns></returns>
	private GUI_HackingNode GetUIComponentOfNode(HackingNode node)
	{
		return nodeUIObjects.Find(x => x.HackNode.Equals(node));
	}

	private void GenerateInputNodeUI()
	{
		int numNodes = inputNodes.Count();
		HorizontalLayoutGroup layout = inputsLayout.GetComponentInChildren<HorizontalLayoutGroup>();
		float cellSizeX = nodeCellSize.x;
		float layoutWidth = inputsLayout.rect.width;
		float spacing = (layoutWidth - cellSizeX * numNodes) / numNodes;

		layout.spacing = spacing;

		foreach (HackingNode node in inputNodes)
		{
			GameObject nodeUIObject = Instantiate(inputHackingNodeUIPrefab, inputsLayout.transform);
			RectTransform nodeRect = nodeUIObject.transform as RectTransform;
			nodeRect.sizeDelta = nodeCellSize;

			GUI_HackingNode nodeGUI = nodeUIObject.GetComponent<GUI_HackingNode>();
			nodeGUI.SetHackingNode(node);

			inputNodeUIObjects.Add(nodeGUI);
			nodeUIObjects.Add(nodeGUI);
		}

	}

	private void GenerateOutputNodeUI()
	{
		int numNodes = outputNodes.Count();
		HorizontalLayoutGroup layout = outputsLayout.GetComponentInChildren<HorizontalLayoutGroup>();
		float cellSizeX = nodeCellSize.x;
		float layoutWidth = inputsLayout.rect.width;
		float spacing = (layoutWidth - cellSizeX * numNodes)/numNodes;

		layout.spacing = spacing;

		foreach (HackingNode node in outputNodes)
		{
			GameObject nodeUIObject = Instantiate(outputHackingNodeUIPrefab, outputsLayout.transform);
			RectTransform nodeRect = nodeUIObject.transform as RectTransform;
			nodeRect.sizeDelta = nodeCellSize;

			GUI_HackingNode nodeGUI = nodeUIObject.GetComponent<GUI_HackingNode>();
			nodeGUI.SetHackingNode(node);

			outputNodeUIObjects.Add(nodeGUI);
			nodeUIObjects.Add(nodeGUI);
		}
	}

	private void GenerateNodeConnections()
	{
		foreach (HackingNode node in outputNodes)
		{
			foreach (HackingNode subNode in node.ConnectedInputNodes)
			{
				GUI_HackingNode outputUINode = GetUIComponentOfNode(node);
				GUI_HackingNode inputUINode = GetUIComponentOfNode(subNode);

				GameObject connectingWire = Instantiate(connectingWireUIPrefab, transform);
				GUI_HackingWire GUIWire = connectingWire.GetComponent<GUI_HackingWire>();
				GUIWire.SetStartUINode(outputUINode);
				GUIWire.SetEndUINode(inputUINode);
				GUIWire.PositionWireBody();

				hackingWires.Add(GUIWire);
			}
		}
	}

	public void RegenerateWiring()
	{
		foreach (GUI_HackingWire wire in hackingWires)
		{
			Destroy(wire.gameObject);
		}
		hackingWires = new List<GUI_HackingWire>();

		GenerateNodeConnections();
	}

	public void RemoveWire(GUI_HackingWire wireUI)
	{
		HackingNode outputNode = wireUI.StartNode.HackNode;
		HackingNode inputNode = wireUI.EndNode.HackNode;

		outputNode.RemoveConnectedNode(inputNode);
			
		ClientRecalculateConnectionList();

		//If we're on client, network to the server the changes we made.
		if (!IsServer)
		{
			int outIndex = hackNodes.IndexOf(outputNode);
			int inIndex = hackNodes.IndexOf(inputNode);
			int[] connectionToRemove = { outIndex, inIndex };
			Debug.Log(connectionToRemove);
			Debug.Log("Output Index: " + outIndex + " Input Index : " + inIndex);
			RemoveHackingConnection.Send(PlayerManager.LocalPlayerScript.gameObject, hackProcess.gameObject, connectionToRemove);
			Debug.Log("Client attempting to remove wire.");
		}

		hackingWires.Remove(wireUI);
		Destroy(wireUI.gameObject);
	}

	/// <summary>
	/// This is called when the client changes something about the connection list. Don't trust this output! Only using it to predict UI changes.
	/// </summary>
	private void ClientRecalculateConnectionList()
	{
		connectionList = GetNodeConnectionList();
		Debug.Log("Printing client connection lits:");
		Debug.Log(connectionList);
	}

	/// <summary>
	/// This method is called by a net message from the server. The client predicts actions will have the determined effect and then this function will
	/// update the UI if there's a mistmatch.
	/// </summary>
	/// <param name="serverConnections"></param>
	public void UpdateConnectionList(List<int[]> serverConnections)
	{
		Debug.Log("Received new list from server, checking if list is the same.");
		bool sameList = Enumerable.SequenceEqual(connectionList, serverConnections);
		if (!sameList)
		{
			Debug.Log("Connection list mismatch, reloading on client.");
			connectionList = serverConnections;
			UpdateNodeConnectionsFromConnectionList();
			RegenerateWiring();
		}
	}

	/// <summary>
	/// Update what nodes are connected to what using the connection list.
	/// </summary>
	private void UpdateNodeConnectionsFromConnectionList()
	{
		foreach (HackingNode node in hackNodes)
		{
			node.RemoveAllConnectedNodes();
		}

		foreach (int[] connection in connectionList)
		{
			HackingNode outputNode = hackNodes[connection[0]];
			HackingNode inputNode = hackNodes[connection[1]];

			outputNode.AddConnectedNode(inputNode);
		}
	}

	public  List<int[]> GetNodeConnectionList()
	{
		List<int[]> connectionList = new List<int[]>();
		List<HackingNode> hackingNodes = hackNodes;
		int outputIndex = 0;
		foreach (HackingNode node in hackingNodes)
		{
			List<HackingNode> connectedNodes = node.ConnectedInputNodes;
			foreach (HackingNode connectedNode in connectedNodes)
			{
				int inputIndex = hackingNodes.IndexOf(connectedNode);
				int[] connection = { outputIndex, inputIndex };
				connectionList.Add(connection);
				Debug.Log(connection);
				Debug.Log("Output Index: " + outputIndex + " Input Index : " + inputIndex);
			}
			outputIndex++;
		}
		return connectionList;
	}

	public void BeginAddingWire(GUI_HackingNode outputNode)
	{
		newWireOutput = outputNode;
		isAddingWire = true;
	}

	public void FinishAddingWire(GUI_HackingNode inputNode)
	{
		newWireInput = inputNode;

		newWireOutput.HackNode.AddConnectedNode(newWireInput.HackNode);

		ClientRecalculateConnectionList();

		if (!IsServer)
		{
			int outIndex = hackNodes.IndexOf(newWireOutput.HackNode);
			int inIndex = hackNodes.IndexOf(newWireInput.HackNode);
			int[] connectionToAdd = { outIndex, inIndex };
			AddHackingConnection.Send(PlayerManager.LocalPlayerScript.gameObject, hackProcess.gameObject, connectionToAdd);

			Debug.Log("Client attempting to add wire.");
		}

		RegenerateWiring();

		isAddingWire = false;

	}
}
