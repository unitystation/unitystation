using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Component to be attached to items to allow them to be used when hacking panels. A hacking device can be an input, an output or both.
/// </summary>
// public class HackingDevice : MonoBehaviour
// {
// 	//private HackingNode inputNode;
//
//
// 	[SerializeField]
// 	private UnityEvent onInputReceived = null;
//
// 	private void OnEnable()
// 	{
//
// 		if (CustomNetworkManager.IsServer)
// 		{
// 			inputNode.AddToInputMethods(onInputReceived.Invoke);
// 		}
// 	}
//
// 	public void SendOutputSignal()
// 	{
// 		outputNode.SendOutputToConnectedNodes();
// 	}
//}
