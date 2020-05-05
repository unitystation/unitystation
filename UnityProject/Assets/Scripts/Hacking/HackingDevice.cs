using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component to be attached to items to allow them to be used when hacking panels. A hacking device can be an input, an output or both.
/// </summary>
public class HackingDevice : MonoBehaviour, IServerSpawn, IClientSpawn
{
	private HackingNode inputNode;
	public HackingNode InputNode => inputNode;

	private HackingNode outputNode;
	public HackingNode OutputNode => outputNode;

	[SerializeField]
	private UnityEvent onInputReceived;

	public void SendOutputSignal()
	{
		outputNode.SendOutputToConnectedNodes();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		inputNode = new HackingNode();
		inputNode.AddToInputMethods(onInputReceived.Invoke);
		inputNode.HiddenLabel = "Input";
		inputNode.IsInput = true;
		inputNode.IsDeviceNode = true;
		outputNode = new HackingNode();
		outputNode.HiddenLabel = "Output";
		outputNode.IsOutput = true;
		outputNode.IsDeviceNode = true;
	}

	public void OnSpawnClient(ClientSpawnInfo info)
	{
		if (!CustomNetworkManager.IsServer)
		{
			inputNode = new HackingNode();
			inputNode.HiddenLabel = "Input";
			inputNode.IsInput = true;
			inputNode.IsDeviceNode = true;
			outputNode = new HackingNode();
			outputNode.HiddenLabel = "Output";
			outputNode.IsOutput = true;
			outputNode.IsDeviceNode = true;
		}
	}
}
