using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component to be attached to items to allow them to be used when hacking panels. A hacking device can be an input, an output or both.
/// </summary>
public class HackingDevice : MonoBehaviour
{
	private HackingNode inputNode;
	public HackingNode InputNode => inputNode;

	private HackingNode outputNode;
	public HackingNode OutputNode => outputNode;

	[SerializeField]
	private UnityEvent onInputReceived = null;

	private void OnEnable()
	{
		inputNode = new HackingNode();
		inputNode.HiddenLabel = "Input";
		inputNode.IsInput = true;
		inputNode.IsDeviceNode = true;
		outputNode = new HackingNode();
		outputNode.HiddenLabel = "Output";
		outputNode.IsOutput = true;
		outputNode.IsDeviceNode = true;
		if (CustomNetworkManager.IsServer)
		{
			inputNode.AddToInputMethods(onInputReceived.Invoke);
		}
	}

	public void SendOutputSignal()
	{
		outputNode.SendOutputToConnectedNodes();
	}
}
