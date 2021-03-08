
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

/// <summary>
/// This component lives on the parent of the object that has the Matrix component, same one that has the actual NetworkIdentity of the matrix
/// (the object that has the Matrix component.
/// The only purpose is to solve issues related to net ID initialization. There are some circumstances where the other components which depend
/// on this object's net ID are initialized
/// before the NetId of this object is assigned, so this component ensures that each register tile and
/// other dependent objects are informed
/// of the correct parent matrix net ID as soon as it becomes available.
/// </summary>
public class NetworkedMatrix : NetworkBehaviour
{

	/// <summary>
	/// Mapping from networked matrix net ID to the events that should be invoked for that net ID when
	/// networking is initialized for that matrix. Events will be cleared after firing once.
	/// </summary>
	private static readonly Dictionary<uint, NetworkedMatrixInitEvent> NetInitActions = new Dictionary<uint, NetworkedMatrixInitEvent>();

	/// <summary>
	/// Contains the IDs of all networked matrices which have already initialized.
	/// </summary>
	private static readonly Dictionary<uint, NetworkedMatrix> InitializedMatrices = new Dictionary<uint, NetworkedMatrix>();

	/// <summary>
	/// Gets a unity event that the caller can subscribe to which will be fired once
	/// the networking for this matrix is initialized.
	/// </summary>
	/// <param name="networkedMatrixNetId"></param>
	/// <returns></returns>
	private static NetworkedMatrixInitEvent WaitForNetworkingInit(uint networkedMatrixNetId)
	{
		NetInitActions.TryGetValue(networkedMatrixNetId, out var unityEvent);
		if (unityEvent == null)
		{
			unityEvent = new NetworkedMatrixInitEvent();
			NetInitActions[networkedMatrixNetId] = unityEvent;
		}

		return unityEvent;
	}

	/// <summary>
	/// Calls toInvoke when the networked matrix with the given net ID is fully initialized. Will be called
	/// immediately if it's already initialized.
	/// </summary>
	/// <param name="networkedMatrixNetId"></param>
	/// <param name="toInvoke"></param>
	public static void InvokeWhenInitialized(uint networkedMatrixNetId, Action<NetworkedMatrix> toInvoke)
	{
		if (networkedMatrixNetId == NetId.Empty || networkedMatrixNetId == NetId.Invalid)
		{
			Logger.LogWarning("Attempted to wait on invalid / empty networked matrix net ID. This might be a bug.", Category.Matrix);
			return;
		}

		//fire immediately if matrix is already initialized
		InitializedMatrices.TryGetValue(networkedMatrixNetId, out var networkedMatrix);
		if (networkedMatrix != null)
		{
			toInvoke.Invoke(networkedMatrix);
		}
		else
		{
			//wait until initialized
			WaitForNetworkingInit(networkedMatrixNetId).AddListener(toInvoke.Invoke);
		}
	}

	/// <summary>
	/// For internal use only, clears out the event dictionary.
	/// </summary>
	public static void _ClearInitEvents()
	{
		foreach (var unityEvent in NetInitActions.Values)
		{
			unityEvent.RemoveAllListeners();
		}
		NetInitActions.Clear();
		InitializedMatrices.Clear();
	}


	private void FireInitEvents()
	{
		//fire once and remove all hooks / the event
		NetInitActions.TryGetValue(netId, out var unityEvent);
		if (unityEvent == null) return;
		unityEvent.Invoke(this);
		unityEvent.RemoveAllListeners();
		NetInitActions.Remove(netId);
	}

	public override void OnStartServer()
	{
		if (!InitializedMatrices.ContainsKey(netId))
		{
			InitializedMatrices.Add(netId, this);
		}

		//ensure all register tiles in this matrix have the correct net id
		foreach (var rt in GetComponentsInChildren<RegisterTile>())
		{
			rt.ServerSetNetworkedMatrixNetID(netId);
		}

		FireInitEvents();
	}

	public override void OnStartClient()
	{
		if (!InitializedMatrices.ContainsKey(netId))
		{
			InitializedMatrices.Add(netId, this);
		}
		//make sure layer orientations are refreshed now that this matrix is initialized
		foreach (var layer in GetComponentsInChildren<Layer>())
		{
			layer.InitFromMatrix();
		}

		FireInitEvents();
	}
}

class NetworkedMatrixInitEvent : UnityEvent<NetworkedMatrix>
{
}
