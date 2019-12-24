
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

/// <summary>
/// This component lives on the parent transform of Matrix, aka the grandparent matrix. The only purpose is to solve
/// issues related to net ID initialization. There are some circumstances where the other components which depend
/// on this object's net ID are initialized
/// before the NetId of this object is assigned, so this component ensures that each register tile and
/// other dependent objects are informed
/// of the correct parent matrix net ID as soon as it becomes available.
/// </summary>
public class GrandparentMatrix : NetworkBehaviour
{

	/// <summary>
	/// Mapping from grandparent matrix net ID to the events that should be invoked for that net ID when
	/// networking is initialized for that matrix. Events will be cleared after firing once.
	/// </summary>
	private static readonly Dictionary<uint, GrandparentMatrixInitEvent> NetInitActions = new Dictionary<uint, GrandparentMatrixInitEvent>();

	/// <summary>
	/// Contains the IDs of all grandparent matrices which have already initialized.
	/// </summary>
	private static readonly Dictionary<uint, GrandparentMatrix> InitializedMatrices = new Dictionary<uint, GrandparentMatrix>();

	/// <summary>
	/// Gets a unity event that the caller can subscribe to which will be fired once
	/// the networking for this matrix is initialized.
	/// </summary>
	/// <param name="grandparentMatrixNetId"></param>
	/// <returns></returns>
	private static GrandparentMatrixInitEvent WaitForNetworkingInit(uint grandparentMatrixNetId)
	{
		NetInitActions.TryGetValue(grandparentMatrixNetId, out var unityEvent);
		if (unityEvent == null)
		{
			unityEvent = new GrandparentMatrixInitEvent();
			NetInitActions[grandparentMatrixNetId] = unityEvent;
		}

		return unityEvent;
	}

	/// <summary>
	/// Calls toInvoke when the grandparent matrix with the given net ID is fully initialized. Will be called
	/// immediately if it's already initialized.
	/// </summary>
	/// <param name="grandparentMatrixNetId"></param>
	/// <param name="toInvoke"></param>
	public static void InvokeWhenInitialized(uint grandparentMatrixNetId, Action<GrandparentMatrix> toInvoke)
	{
		if (grandparentMatrixNetId == NetId.Empty || grandparentMatrixNetId == NetId.Invalid)
		{
			Logger.LogWarning("Attempted to wait on invalid / empty grandparent matrix net ID. This might be a bug.");
			return;
		}

		//fire immediately if matrix is already initialized
		InitializedMatrices.TryGetValue(grandparentMatrixNetId, out var grandparentMatrix);
		if (grandparentMatrix != null)
		{
			toInvoke.Invoke(grandparentMatrix);
		}
		else
		{
			//wait until initialized
			WaitForNetworkingInit(grandparentMatrixNetId).AddListener(toInvoke.Invoke);
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
			rt.ServerSetGrandparentMatrixNetID(netId);
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

class GrandparentMatrixInitEvent : UnityEvent<GrandparentMatrix>
{
}
