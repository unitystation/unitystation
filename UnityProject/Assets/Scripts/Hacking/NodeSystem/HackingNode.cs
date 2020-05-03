using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

/// <summary>
/// Nodes to be used by the hacking system. They are used to direction process flow of objects that utilise them, while being modifiable hacking
/// the object itself.
/// </summary>
public class HackingNode
{
	/// <summary>
	/// The public label is what a player sees when they view the name of this node in the hacking menu. This is important to have synced, as it should be the same serverside and clientside, and late joiners need to have the
	/// correct public label to be on the same page as other players.
	/// </summary>
	public string PublicLabel { get; set; }

	//Using an auto get-set for this for now. Will need to update how this functions when I figure out how the internal label should be shared. i.e. to the engineers somehow.`
	public	string HiddenLabel { get; set; }

	public string InternalIdentifier { get; set; }

	//Bools for whether the node is an input or output node. I'm not exactly sure how these will work, so maybe eventually there could be a situation where a node is both? Dunno
	public	bool IsInput { get; set; }

	public bool IsOutput { get; set; }

	public bool IsDeviceNode { get; set; }

	private List<HackingNode> connectedInputNodes = new List<HackingNode>();

	public List<HackingNode> ConnectedInputNodes => connectedInputNodes;


	public Action<GameObject> inputCallBacks;

	public Action<GameObject> onConnectionCut;

	//This does not need to be synced between the server and client. As long as the server is sending correct data to the client, the object this is attached to should work fine.
	//We only need to update the client on what nodes are doing what when they're in the hacking menu.
	//private NodeInputCallback inputMethods = null;

	public HackingNode(string publicLabel, string internalLabel, bool isInput, bool isOutput)
	{
		PublicLabel = publicLabel;
		InternalIdentifier = internalLabel;
		IsInput = isInput;
		IsOutput = isOutput;
		IsDeviceNode = false;
	}

	public HackingNode()
	{
		PublicLabel = "unset label";
		InternalIdentifier = "unset internal label";
		IsInput = false;
		IsOutput = false;
		IsDeviceNode = false;
	}

	public static HackingNode GetNodeByInternalLabel(List<HackingNode> nodeList, string internalLabel)
	{
		return nodeList.Find(x => x.InternalIdentifier.Equals(internalLabel));
	}

	/// <summary>
	/// Called when the node receives an input from another node, if this node is an input node.
	/// </summary>
	public virtual void InputReceived(GameObject originator = null)
	{
		inputCallBacks?.Invoke(originator);
	}

	/// <summary>
	/// Causes the node to send output to all other nodes this node is connected to, if they are input nodes.
	/// </summary>
	public virtual void SendOutputToConnectedNodes(GameObject originator = null)
	{
		foreach (HackingNode node in connectedInputNodes)
		{
			node.InputReceived(originator);
		}
	}

	public virtual void AddWireCutCallback(Action method)
	{
		Action<GameObject> methodWrapped = delegate (GameObject gameObject)
		{
			method();
		};

		onConnectionCut += methodWrapped;
	}

	public virtual void AddWireCutCallback(Action<GameObject> method)
	{
		onConnectionCut += method;
	}

	public virtual void WireCutCallback(GameObject obj = null)
	{
		onConnectionCut?.Invoke(obj);
	}

	public virtual void AddConnectedNode(HackingNode node)
	{
		connectedInputNodes.Add(node);
	}

	public virtual void RemoveConnectedNode(HackingNode node)
	{
		connectedInputNodes.Remove(node);
	}

	public virtual void RemoveAllConnectedNodes()
	{
		connectedInputNodes.Clear();
	}

	/// <summary>
	/// Adds a method to the input method delegates. Basically, allows a way to callback a function added to the node.
	/// </summary>
	/// <param name="method"></param>
	public virtual void AddToInputMethods(Action<GameObject> method)
	{
		inputCallBacks += method;
	}

	public virtual void AddToInputMethods(Action method)
	{
		Action<GameObject> methodWrapped = delegate (GameObject gameObject)
		{
			method();
		};

		inputCallBacks += methodWrapped;
	}

	public virtual void RemoveFromInputMethods(Action<GameObject> method)
	{
		inputCallBacks -= method;
	}

	public virtual void RemoveFromInputMethods(Action method)
	{
		Action<GameObject> methodWrapped = delegate (GameObject gameObject)
		{
			method();
		};

		inputCallBacks -= methodWrapped;
	}
}
