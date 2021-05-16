
using System;
using System.Collections.Generic;
using Mirror;
using Shuttles;
using UnityEditor;
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
public class NetworkedMatrix : MonoBehaviour
{
	[HideInInspector]
	public ulong networkedMatrixSceneId;

	/// <summary>
	/// Keep track of all networkedMatrixSceneIds to detect scene duplicates
	/// </summary>
	private static readonly Dictionary<ulong, NetworkedMatrix> networkedMatrixSceneIds = new Dictionary<ulong, NetworkedMatrix>();

	/// <summary>
	/// Mapping from networked matrix net ID to the events that should be invoked for that net ID when
	/// networking is initialized for that matrix. Events will be cleared after firing once.
	/// </summary>
	private static readonly Dictionary<uint, NetworkedMatrixInitEvent> NetInitActions = new Dictionary<uint, NetworkedMatrixInitEvent>();

	/// <summary>
	/// Contains the IDs of all networked matrices which have already initialized.
	/// </summary>
	private static readonly Dictionary<uint, NetworkedMatrix> InitializedMatrices = new Dictionary<uint, NetworkedMatrix>();

	[HideInInspector]
	public MatrixSync MatrixSync;

	#region Networked Matrix SceneIds
#if UNITY_EDITOR
	//A copy of how mirror creates scene ids for network identities but this is for network matrixes instead
		private void OnValidate()
		{
			if(Application.isPlaying) return;

			AssignSceneID();
		}

		void AssignSceneID()
        {
            // we only ever assign sceneIds at edit time, never at runtime.
            // by definition, only the original scene objects should get one.
            // -> if we assign at runtime then server and client would generate
            //    different random numbers!
            if (Application.isPlaying)
                return;

            // no valid sceneId yet, or duplicate?
            bool duplicate = networkedMatrixSceneIds.TryGetValue(networkedMatrixSceneId, out NetworkedMatrix networkedMatrix) && networkedMatrix != null && networkedMatrix != this;
            if (networkedMatrixSceneId == 0 || duplicate)
            {
                // clear in any case, because it might have been a duplicate
                networkedMatrixSceneId = 0;

                // if a scene was never opened and we are building it, then a
                // sceneId would be assigned to build but not saved in editor,
                // resulting in them getting out of sync.
                // => don't ever assign temporary ids. they always need to be
                //    permanent
                // => throw an exception to cancel the build and let the user
                //    know how to fix it!
                if (BuildPipeline.isBuildingPlayer)
                    throw new InvalidOperationException("Scene " + gameObject.scene.path + " needs to be opened and resaved before building, because the scene object " + name + " has no valid networkedMatrix sceneId yet.");

                // if we generate the sceneId then we MUST be sure to set dirty
                // in order to save the scene object properly. otherwise it
                // would be regenerated every time we reopen the scene, and
                // upgrading would be very difficult.
                // -> Undo.RecordObject is the new EditorUtility.SetDirty!
                // -> we need to call it before changing.
                Undo.RecordObject(this, "Generated Networked Matrix SceneId");

                // generate random sceneId part (0x00000000FFFFFFFF)
                uint randomId = Utils.GetTrueRandomUInt();

                // only assign if not a duplicate of an existing scene id
                // (small chance, but possible)
                duplicate = networkedMatrixSceneIds.TryGetValue(randomId, out networkedMatrix) && networkedMatrix != null && networkedMatrix != this;
                if (!duplicate)
                {
	                networkedMatrixSceneId = randomId;
                    //Debug.Log(name + " in scene=" + gameObject.scene.name + " sceneId assigned to: " + m_SceneId.ToString("X"));
                }
            }

            // add to sceneIds dict no matter what
            // -> even if we didn't generate anything new, because we still need
            //    existing sceneIds in there to check duplicates
            networkedMatrixSceneIds[networkedMatrixSceneId] = this;
        }
#endif

		// copy scene path hash into sceneId for scene objects.
		// this is the only way for scene file duplication to not contain
		// duplicate sceneIds as it seems.
		// -> sceneId before: 0x00000000AABBCCDD
		// -> then we clear the left 4 bytes, so that our 'OR' uses 0x00000000
		// -> then we OR the hash into the 0x00000000 part
		// -> buildIndex is not enough, because Editor and Build have different
		//    build indices if there are disabled scenes in build settings, and
		//    if no scene is in build settings then Editor and Build have
		//    different indices too (Editor=0, Build=-1)
		// => ONLY USE THIS FROM POSTPROCESSSCENE!
		public void SetSceneIdSceneHashPartInternal()
		{
			// Use `ToLower` to that because BuildPipeline.BuildPlayer is case insensitive but hash is case sensitive
			// If the scene in the project is `forest.unity` but `Forest.unity` is given to BuildPipeline then the
			// BuildPipeline will use `Forest.unity` for the build and create a different hash than the editor will.
			// Using ToLower will mean the hash will be the same for these 2 paths
			// Assets/Scenes/Forest.unity
			// Assets/Scenes/forest.unity
			string scenePath = gameObject.scene.path.ToLower();

			// get deterministic scene hash
			uint pathHash = (uint)scenePath.GetStableHashCode();

			// shift hash from 0x000000FFFFFFFF to 0xFFFFFFFF00000000
			ulong shiftedHash = (ulong)pathHash << 32;

			// OR into scene id
			networkedMatrixSceneId = (networkedMatrixSceneId & 0xFFFFFFFF) | shiftedHash;

			// log it. this is incredibly useful to debug sceneId issues.
			// Debug.Log(name + " in scene=" + gameObject.scene.name + " scene index hash(" + pathHash.ToString("X") + ") copied into sceneId: " + sceneId.ToString("X"));
		}

		public static NetworkedMatrix GetNetworkedMatrixForId(ulong id)
		{
			if (networkedMatrixSceneIds.TryGetValue(id, out var networkedMatrix))
			{
				return networkedMatrix;
			}

			return null;
		}

	#endregion

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
		NetInitActions.TryGetValue(MatrixSync.netId, out var unityEvent);
		if (unityEvent == null) return;
		unityEvent.Invoke(this);
		unityEvent.RemoveAllListeners();
		NetInitActions.Remove(MatrixSync.netId);
	}

	private void Start()
	{
		if (CustomNetworkManager.IsServer)
		{
			OnStartServer();
		}

		//Matrixes cannot be networked as a message to spawn an object beneath it can happen before the matrix has activated
		if (GetComponent<NetworkIdentity>() != null)
		{
			Logger.LogError($"{gameObject.name} has a network identity please remove it, matrixes cannot be networked objects");
		}
	}

	private void OnStartServer()
	{
		if (MatrixSync == null)
		{
			var result = Spawn.ServerPrefab(MatrixManager.Instance.MatrixSyncPrefab, transform.position, transform);

			if (result.Successful == false)
			{
				Logger.LogError($"Failed to spawn matrix sync for {gameObject.name}");
				return;
			}

			result.GameObject.GetComponent<MatrixSync>().SetMatrixId(networkedMatrixSceneId);
		}

		if (!InitializedMatrices.ContainsKey(MatrixSync.netId))
		{
			InitializedMatrices.Add(MatrixSync.netId, this);
		}

		//ensure all register tiles in this matrix have the correct net id
		foreach (var rt in GetComponentsInChildren<RegisterTile>())
		{
			rt.ServerSetNetworkedMatrixNetID(MatrixSync.netId);
		}

		FireInitEvents();
	}

	public void OnStartClient()
	{
		if (!InitializedMatrices.ContainsKey(MatrixSync.netId))
		{
			InitializedMatrices.Add(MatrixSync.netId, this);
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
