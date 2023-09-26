using System;
using System.Collections.Generic;
using _3D;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Mirror;
using Core.Editor.Attributes;
using Logs;
using Objects;
using Tilemaps.Behaviours.Layers;
using Systems.Electricity;
using Systems.Pipes;
using Util;

public enum ObjectType
{
	Item,
	Object,
	Player,
	Wire
}

public interface IRegisterTileInitialised
{
	public void OnRegisterTileInitialised(RegisterTile registerTile);
}

/// <summary>
/// Holds various behavior which affects the tile the object is currently on.
/// A given tile in the ObjectLayer (which represents an individual square in the game world)
/// can have multiple gameobjects with RegisterTile behavior. This lets each gameobject on the tile
/// influence how the tile works (such as making it impassible)
///
/// Also tracks the Matrix the object is in.
/// </summary>
[ExecuteInEditMode]
public class RegisterTile : NetworkBehaviour, IServerDespawn
{
	//relationships which only need to be checked when UpdatePosition methods are called
	private List<BaseSpatialRelationship> sameMatrixRelationships;

	//relationships which need to be checked via polling due to being on different matrices
	private List<BaseSpatialRelationship> crossMatrixRelationships;

	[Tooltip("For debug purposes only. Logs trace-level debug messages in " +
	         "Matrix logging category for this particular" +
	         " object so it's easier to see what's happening to it.")]
	public bool matrixDebugLogging;

	/// <summary>
	/// Object layer this gameobject is in (all registertiles live in an object layer).
	/// </summary>
	private ObjectLayer objectLayer;

	/// <summary>
	/// Tile change manager of the matrix this object is on.
	/// </summary>
	public TileChangeManager TileChangeManager => Matrix ? Matrix.TileChangeManager : null;

	[SerializeField, FormerlySerializedAs("ObjectType") ]
	[Tooltip("The kind of object this is.")]
	private ObjectType objectType = ObjectType.Item;

	/// <summary>
	/// The kind of object this is.
	/// </summary>
	public ObjectType ObjectType => objectType;

	/// <summary>
	/// Matrix this object lives in
	/// </summary>
	public Matrix Matrix { get; private set; }

	public bool LiesFlat3D = false;

	/// <summary>
	/// Invoked when the parent net ID of this RegisterTile has changed, after reparenting
	/// has been performed in RegisterTile (which updates the parent net ID, parent transform, parent
	/// matrix, position, object layer, and parent matrix move if one is present in the matrix).
	/// Allows components to respond to this event and do additional processing
	/// based on the new parent net ID.
	/// </summary>
	[NonSerialized] public UnityEvent OnParentChangeComplete = new UnityEvent();

	/// <summary>
	/// Invoked clientside when object is in the world (not at hidden pos) and then disappears for whatever reason
	/// (registered to hidden pos)
	/// </summary>
	[NonSerialized] public UnityEvent OnDisappearClient = new UnityEvent();

	/// <summary>
	/// Invoked clientside when object is invisible to client (at hidden pos) and then becomes visible
	/// (not at hidden pos)
	/// </summary>
	[NonSerialized] public UnityEvent OnAppearClient = new UnityEvent();

	/// <summary>
	/// Invoked serverside when object is despawned
	/// </summary>
	[NonSerialized] public UnityEvent OnDespawnedServer = new UnityEvent();


	[SyncVar(hook = nameof(SyncNetworkedMatrixNetId))]
	private uint networkedMatrixNetId;

	/// <summary>
	/// NetId of our current networked matrix. Note this id is on the parent of the
	/// Matrix gameObject, i.e. the one with NetworkedMatrix not the one with Matrix, hence calling
	/// it "networked matrix".
	/// </summary>
	public uint NetworkedMatrixNetId => networkedMatrixNetId;

	/// <summary>
	/// Returns the correct client/server version of world position depending on if this is
	/// called on client or server.
	/// </summary>
	public Vector3Int WorldPosition
	{
		get
		{
			if (objectPhysics.HasComponent == false)
			{
				objectPhysics.ResetComponent(gameObject);
			}

			if (objectPhysics.HasComponent == false)
			{
				return gameObject.AssumedWorldPosServer().RoundToInt();
			}

			return objectPhysics.Component.OfficialPosition.RoundToInt();
		}
	}

	public Vector3Int WorldPositionServer
	{
		get
		{
			if (objectPhysics.HasComponent == false)
			{
				objectPhysics.ResetComponent(gameObject);
			}
			return objectPhysics.Component.OfficialPosition.RoundToInt();
		}
	}

	/// <summary>
	/// Registered local position of this object. Returns correct value depending on if this is on the
	/// server or client.
	/// </summary>
	public Vector3Int LocalPosition => isServer ? LocalPositionServer : LocalPositionClient;

	/// <summary>
	/// the "registered" local position of this object (which might differ from transform.localPosition).
	/// It will be set to TransformState.HiddenPos when hiding the object.
	/// </summary>
	public Vector3Int LocalPositionServer { get; private set; }

	public Vector3Int LocalPositionClient { get; private set; }

	/// <summary>
	/// Event invoked on server side when position changes. Passes the new local position in the matrix.
	/// </summary>
	[NonSerialized] public readonly Vector3IntEvent OnLocalPositionChangedServer = new Vector3IntEvent();

	private IMatrixRotation[] matrixRotationHooks;

	//cached for fast fire exposure without gc
	private IFireExposable[] fireExposables;

	public IPlayerEntersTile[] IPlayerEntersTiles;

	public IObjectEntersTile[] IObjectEntersTiles;

	[SerializeField] private PrefabTracker prefabTracker;
	public PrefabTracker PrefabTracker => prefabTracker;

	private ElectricalOIinheritance electricalData;
	public ElectricalOIinheritance ElectricalData => electricalData;

	private PipeData pipeData;
	public PipeData PipeData => pipeData;

	private CheckedComponent<UniversalObjectPhysics> objectPhysics = new CheckedComponent<UniversalObjectPhysics>();
	public CheckedComponent<UniversalObjectPhysics> ObjectPhysics => objectPhysics;

	[SerializeField ]
	private SortingGroup CurrentsortingGroup;

	private bool Initialized;

	public bool Active { get; private set; } = true;

	#region Lifecycle

	protected virtual void Awake()
	{
		LocalPositionServer = TransformState.HiddenPos;
		LocalPositionClient = TransformState.HiddenPos;
		if (transform.parent) //clients dont have this set yet
		{
			objectLayer = transform.parent.GetComponent<ObjectLayer>() ?? transform.parent.GetComponentInParent<ObjectLayer>();
		}
		objectPhysics.ResetComponent(this);
		matrixRotationHooks = GetComponents<IMatrixRotation>();
		fireExposables = GetComponents<IFireExposable>();
		IPlayerEntersTiles = GetComponents<IPlayerEntersTile>();
		IObjectEntersTiles = GetComponents<IObjectEntersTile>();
		CurrentsortingGroup = GetComponent<SortingGroup>();

		if (Manager3D.Is3D && GameData.IsHeadlessServer == false )
		{
			var convertTo3d = this.gameObject.GetComponent<ConvertTo3D>();
			if (convertTo3d  == null)
			{

				convertTo3d = gameObject.AddComponent<ConvertTo3D>();

			}
			convertTo3d .DoConvertTo3D();
		}
		else
		{
			transform.localRotation = Quaternion.Euler(0, 0, transform.localRotation.eulerAngles.z);
		}
	}

	public override void OnStartServer()
	{
		var matrix = transform.parent.GetComponentInParent<Matrix>();
		if (matrix.Initialized)
		{
			Initialize(matrix);
		}
	}

	public override void OnStartClient()
	{
		if (isServer)
			return;

		if (transform.parent == null) //object spawned mid-round
		{
			NetworkedMatrix.InvokeWhenInitialized(networkedMatrixNetId, ClientLoading);
		}
		else
		{
			var matrix = transform.parent.GetComponentInParent<Matrix>();
			if (matrix.Initialized)
			{
				Initialize(matrix);
			}
		}
	}

	public void ClientLoading(NetworkedMatrix networkedMatrix)
	{
		var matrix = networkedMatrix.matrix;
		if (matrix.Initialized)
		{
			Initialize(matrix);
		}
		else
		{
			//will be gathered with a GetComponentsInChildren() and initialized by the matrix
			transform.SetParent(matrix.transform, false);
		}
	}

	public void Initialize(Matrix matrix)
	{
		Matrix = matrix;

		//Prevent interface running more than once
		if (Initialized == false)
		{
			foreach (var registerTileInitialised in GetComponents<IRegisterTileInitialised>())
			{
				registerTileInitialised.OnRegisterTileInitialised(this);
			}
		}

		Initialized = true;
		var networkedMatrix = Matrix.transform.parent.GetComponent<NetworkedMatrix>();
		if (isServer)
		{
			ServerSetNetworkedMatrixNetID(networkedMatrix.MatrixSync.netId);
		}
		else
		{
			FinishNetworkedMatrixRegistration(networkedMatrix);
		}
	}

	public void OnDestroy()
	{
		if (objectLayer)
		{
			objectLayer.ServerObjects.Remove(LocalPositionServer, this);
			objectLayer.ClientObjects.Remove(LocalPositionClient, this);
		}

		Matrix?.MatrixMove?.MatrixMoveEvents?.OnRotate?.RemoveListener(OnRotate);
	}

	public virtual void OnDespawnServer(DespawnInfo info)
	{
		//cancel all relationships
		if (sameMatrixRelationships != null)
		{
			for (int i = sameMatrixRelationships.Count-1; i >= 0; i--)
			{
				var relationship = sameMatrixRelationships[i];
				Loggy.LogTraceFormat("Cancelling spatial relationship {0} because {1} is despawning.",
					Category.SpatialRelationship, relationship, this);
				SpatialRelationship.ServerEnd(relationship);
			}
		}

		if (crossMatrixRelationships != null)
		{
			for (int i = crossMatrixRelationships.Count - 1; i >= 0; i--)
			{
				var relationship = crossMatrixRelationships[i];
				Loggy.LogTraceFormat("Cancelling spatial relationship {0} because {1} is despawning.",
					Category.SpatialRelationship, relationship, this);
				SpatialRelationship.ServerEnd(relationship);
			}
		}

		OnDespawnedServer.Invoke();
	}

	public void ChangeActiveState(bool newState)
	{
		Active = newState;
	}

	#endregion


	public void ServerSetLocalPosition(Vector3Int value, bool overRideCheck = false)
	{
		if (LocalPositionServer == value && overRideCheck == false) return;

		if (objectLayer)
		{
			objectLayer.ServerObjects.Remove(LocalPositionServer, this);
			if (value != TransformState.HiddenPos)
			{
				objectLayer.ServerObjects.Add(value, this);
			}
		}

		LocalPositionServer = value;

		CheckSameMatrixRelationships(); //TODO Might be laggy?
		OnLocalPositionChangedServer.Invoke(LocalPositionServer);
	}

	public void ClientSetLocalPosition(Vector3Int value, bool overRideCheck = false)
	{
		if (LocalPositionClient == value && overRideCheck == false)
			return;
		bool appeared = LocalPositionClient == TransformState.HiddenPos && value != TransformState.HiddenPos;
		bool disappeared = LocalPositionClient != TransformState.HiddenPos && value == TransformState.HiddenPos;
		if (objectLayer)
		{
			objectLayer.ClientObjects.Remove(LocalPositionClient, this);
			if (value != TransformState.HiddenPos)
			{
				objectLayer.ClientObjects.Add(value, this);
			}
		}

		LocalPositionClient = value;

		if (appeared)
		{
			OnAppearClient?.Invoke();
		}

		if (disappeared)
		{
			OnDisappearClient?.Invoke();
		}
	}

	/// <summary>
	/// Set our parent matrix net ID to this.
	/// </summary>
	/// <param name="newNetworkedMatrixNetID"></param>
	[Server]
	public bool ServerSetNetworkedMatrixNetID(uint newNetworkedMatrixNetID)
	{
		if(networkedMatrixNetId == newNetworkedMatrixNetID)
		{
			return false;
		}
		LogMatrixDebug("ServerSetNetworkedMatrixNetID");
		networkedMatrixNetId = newNetworkedMatrixNetID;
		return true;
	}


	/// <summary>
	/// Invoked when networkedMatrixNetId is changed on the server, updating the client's networkedMatrixNetId.
	/// </summary>
	/// <param name="oldNetworkMatrixId"></param>
	/// <param name="newNetworkedMatrixNetID">uint of the new parent</param>
	private void SyncNetworkedMatrixNetId(uint oldNetworkMatrixId, uint newNetworkedMatrixNetID)
	{
		networkedMatrixNetId = newNetworkedMatrixNetID;
		if (hasAuthority && isServer == false) return;
		NetworkedMatrix.InvokeWhenInitialized(networkedMatrixNetId, FinishNetworkedMatrixRegistration); //note: we dont actually wait for init here anymore
	}

	public void FinishNetworkedMatrixRegistration(NetworkedMatrix networkedMatrix)
	{
		if (networkedMatrix == null) return;
		//if we had any spin rotation, preserve it,
		//otherwise all objects should always have upright local rotation
		var rotation = transform.rotation;
		bool hadSpinRotation = Quaternion.Angle(transform.localRotation, Quaternion.identity) > 5;

		var newObjectLayer = networkedMatrix.GetComponentInChildren<ObjectLayer>();
		if (objectLayer != newObjectLayer)
		{
			if (objectLayer)
			{
				objectLayer.ServerObjects.Remove(LocalPositionServer, this);
				objectLayer.ClientObjects.Remove(LocalPositionClient, this);
			}
			objectLayer = newObjectLayer;
		}

		var WorldCashed = transform.position;

		transform.SetParent(objectLayer.transform, true);

		//preserve absolute rotation if there was spin rotation
		if (hadSpinRotation)
		{
			transform.rotation = rotation;
		}
		else
		{
			//objects are always upright w.r.t. parent matrix
			transform.localRotation = Quaternion.identity;
		}

		//this will fire parent change hooks so we do it last
		SetMatrix(networkedMatrix.GetComponentInChildren<Matrix>());

		if (objectPhysics.HasComponent)
		{
			transform.localPosition =  WorldCashed.ToLocal(objectLayer.Matrix);
		}

		UpdatePositionClient();
		UpdatePositionServer();

		OnParentChangeComplete.Invoke();
	}

	private void SetMatrix(Matrix value)
	{
		MatrixChange(Matrix, value);
		if (value)
		{
			//LogMatrixDebug($"Matrix set from {matrix} to {value}");
			if (Matrix != null && Matrix.IsMovable)
			{
				Matrix.MatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnRotate);
			}

			Matrix = value;
			if (Matrix != null && Matrix.IsMovable)
			{
				//LogMatrixDebug($"Registered OnRotate to {matrix}");
				Matrix.MatrixMove.MatrixMoveEvents.OnRotate.AddListener(OnRotate);
				if (isServer)
				{
					OnRotate(new MatrixRotationInfo(Matrix.MatrixMove, Matrix.MatrixMove.FacingOffsetFromInitial,
						NetworkSide.Server, RotationEvent.Register));
				}

				OnRotate(new MatrixRotationInfo(Matrix.MatrixMove, Matrix.MatrixMove.FacingOffsetFromInitial,
					NetworkSide.Client, RotationEvent.Register));
			}


			//setting objects in storage to the same matrix
			if (isServer)
			{
				if (TryGetComponent<ItemStorage>(out var itemStorage))
				{
					foreach (var itemSlot in itemStorage.GetItemSlots())
					{
						if (itemSlot.Item)
						{
							var itemSlotRegisterItem = itemSlot.Item.GetComponent<RegisterItem>();
							itemSlotRegisterItem.Matrix = Matrix;
						}
					}
				}

				if (TryGetComponent<DynamicItemStorage>(out var dynamicItemStorage))
				{
					foreach (var itemSlot in dynamicItemStorage.GetItemSlots())
					{
						if (itemSlot.Item)
						{
							var itemSlotRegisterItem = itemSlot.Item.GetComponent<RegisterItem>();
							itemSlotRegisterItem.Matrix = Matrix;
						}
					}
				}
			}
		}
	}

	public virtual void MatrixChange(Matrix MatrixOld, Matrix MatrixNew)
	{

	}

	public void UnregisterClient()
	{
		ClientSetLocalPosition(TransformState.HiddenPos);
	}

	public void UnregisterServer()
	{
		ServerSetLocalPosition(TransformState.HiddenPos);
	}

	private void OnRotate(MatrixRotationInfo info)
	{
		if (matrixRotationHooks == null) return;
		//pass rotation event on to our children
		foreach (var matrixRotationHook in matrixRotationHooks)
		{
			matrixRotationHook.OnMatrixRotate(info);
		}
	}

	public void UpdatePositionServer()
	{
		var prevPosition = LocalPositionServer;
		ServerSetLocalPosition(transform.localPosition.RoundToInt(), true);
		if (prevPosition != LocalPositionServer)
		{
			OnLocalPositionChangedServer.Invoke(LocalPositionServer);
			CheckSameMatrixRelationships();
		}
	}

	public void UpdatePositionClient()
	{

		ClientSetLocalPosition(transform.localPosition.RoundToInt(), true);

		CheckSameMatrixRelationships();
	}

	/// <summary>
	/// For internal use by the relationship system only. Use SpatialRelationship.Activate instead to
	/// activate a relationship between 2 register tiles
	///
	/// Adds a new spatial relationship which will be checked when this register tile moves relative to the other.
	/// </summary>
	public void _AddSpatialRelationship(BaseSpatialRelationship toAdd)
	{
		//are we across matrices?
		if (toAdd.Other(this).Matrix != Matrix)
		{
			AddCrossMatrixRelationship(toAdd);
		}
		else
		{
			AddSameMatrixRelationship(toAdd);
		}
	}

	/// <summary>
	/// For internal use by the relationship system only. Use SpatialRelationship.Cancel instead to
	/// cancel a pre-existing relationship between 2 register tiles
	///
	/// removes the spatial relationship such that this registertile will no longer check it
	/// </summary>
	public void _RemoveSpatialRelationship(BaseSpatialRelationship toRemove)
	{
		RemoveSameMatrixRelationship(toRemove);
		RemoveCrossMatrixRelationship(toRemove);
	}

	private void CheckSameMatrixRelationships()
	{
		//fires hooks for these relationships and checks if they should be switched to cross-matrix
		if (sameMatrixRelationships != null)
		{
			//keeping null to avoid GC unless a switch happens, which should be rare
			List<BaseSpatialRelationship> toSwitch = null;
			List<BaseSpatialRelationship> toCancel = null;
			foreach (var sameMatrixRelationship in sameMatrixRelationships)
			{
				var cancelled = sameMatrixRelationship.ShouldRelationshipEnd();
				if (cancelled)
				{
					if (toCancel == null)
					{
						toCancel = new List<BaseSpatialRelationship>();
					}

					toCancel.Add(sameMatrixRelationship);
				}
				else
				{
					//not cancelled, check if we moved cross-matrix
					if (sameMatrixRelationship.Other(this).Matrix != this.Matrix)
					{
						if (toSwitch == null)
						{
							toSwitch = new List<BaseSpatialRelationship>();
						}

						toSwitch.Add(sameMatrixRelationship);
					}
				}
			}

			if (toCancel != null)
			{
				foreach (var cancelled in toCancel)
				{
					Loggy.LogTraceFormat("Cancelling spatial relationship {0} because OnRelationshipChanged" +
					                      " returned true.", Category.SpatialRelationship, cancelled);
					SpatialRelationship.ServerEnd(cancelled);
				}
			}

			if (toSwitch != null)
			{
				foreach (var switched in toSwitch)
				{
					Loggy.LogTraceFormat("Switching spatial relationship {0} to cross matrix because" +
					                      " objects moved to different matrices.", Category.SpatialRelationship,
						switched);
					RemoveSameMatrixRelationship(switched);
					AddCrossMatrixRelationship(switched);
				}
			}
		}
	}

	private void AddSameMatrixRelationship(BaseSpatialRelationship toAdd)
	{
		if (sameMatrixRelationships == null)
		{
			sameMatrixRelationships = new List<BaseSpatialRelationship>();
		}

		Loggy.LogTraceFormat("Adding same matrix relationship {0} on {1}",
			Category.SpatialRelationship, toAdd, this);
		sameMatrixRelationships.Add(toAdd);
	}

	private void AddCrossMatrixRelationship(BaseSpatialRelationship toAdd)
	{
		//we only check cross matrix relationships if we are the leader, since only
		//one side needs to poll.
		if (!toAdd.IsLeader(this))
		{
			Loggy.LogTraceFormat("Not adding cross matrix relationship {0} on {1} because {1} is not the leader",
				Category.SpatialRelationship, toAdd, this);
			return;
		}

		Loggy.LogTraceFormat("Adding cross matrix relationship {0} on {1}",
			Category.SpatialRelationship, toAdd, this);

		if (crossMatrixRelationships == null)
		{
			crossMatrixRelationships = new List<BaseSpatialRelationship>();
			UpdateManager.Add(CallbackType.UPDATE, UpdatePollCrossMatrixRelationships);
		}

		crossMatrixRelationships.Add(toAdd);
	}

	private void RemoveSameMatrixRelationship(BaseSpatialRelationship toRemove)
	{
		if (sameMatrixRelationships == null) return;
		Loggy.LogTraceFormat("Removing same matrix relationship {0} from {1}",
			Category.SpatialRelationship, toRemove, this);
		sameMatrixRelationships.Remove(toRemove);
		if (sameMatrixRelationships.Count == 0)
		{
			sameMatrixRelationships = null;
		}
	}

	private void RemoveCrossMatrixRelationship(BaseSpatialRelationship toRemove)
	{
		if (crossMatrixRelationships == null) return;
		Loggy.LogTraceFormat("Removing cross matrix relationship {0} from {1}",
			Category.SpatialRelationship, toRemove, this);
		crossMatrixRelationships.Remove(toRemove);
		if (crossMatrixRelationships.Count == 0)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdatePollCrossMatrixRelationships);
			crossMatrixRelationships = null;
		}
	}

	private void UpdatePollCrossMatrixRelationships()
	{
		//used in update manager when there is a cross matrix relationship
		//that this registertile is the leader for. Checks the relationship and switches it
		//over to same matrix if they both end up on the same matrix

		if (crossMatrixRelationships != null)
		{
			//keeping null to avoid GC unless a switch happens, which should be rare
			List<BaseSpatialRelationship> toSwitch = null;
			List<BaseSpatialRelationship> toCancel = null;
			foreach (var crossMatrixRelationship in crossMatrixRelationships)
			{
				var cancelled = crossMatrixRelationship.ShouldRelationshipEnd();
				if (cancelled)
				{
					if (toCancel == null)
					{
						toCancel = new List<BaseSpatialRelationship>();
					}

					toCancel.Add(crossMatrixRelationship);
				}
				else
				{
					//not cancelled, check if we moved to same matrix
					if (crossMatrixRelationship.Other(this).Matrix == this.Matrix)
					{
						if (toSwitch == null)
						{
							toSwitch = new List<BaseSpatialRelationship>();
						}

						toSwitch.Add(crossMatrixRelationship);
					}
				}
			}

			if (toCancel != null)
			{
				foreach (var cancelled in toCancel)
				{
					Loggy.LogTraceFormat("Cancelling spatial relationship {0} because OnRelationshipChanged" +
					                      " returned true.", Category.SpatialRelationship, cancelled);
					SpatialRelationship.ServerEnd(cancelled);
				}
			}

			if (toSwitch != null)
			{
				foreach (var switched in toSwitch)
				{
					Loggy.LogTraceFormat("Switching spatial relationship {0} to same matrix because" +
					                      " objects moved to the same matrix.", Category.SpatialRelationship, switched);
					RemoveCrossMatrixRelationship(switched);
					AddSameMatrixRelationship(switched);
				}
			}
		}
	}

	public virtual bool IsPassable(bool isServer, GameObject context = null)
	{
		return true;
	}

	///<summary> Is it passable when approaching from outside? </summary>
	public virtual bool IsPassableFromOutside(Vector3Int enteringFrom, bool isServer, GameObject context = null)
	{
		return true;
	}

	/// <summary> Is it passable when trying to leave it? </summary>
	public virtual bool IsPassableFromInside(Vector3Int leavingTo, bool isServer, GameObject context = null)
	{
		return true;
	}

	public virtual bool IsReachableThrough(Vector3Int reachingFrom, bool isServer, GameObject context = null)
	{
		return false;
	}

	public virtual bool DoesNotBlockClick(Vector3Int reachingFrom, bool isServer)
	{
		return true;
	}

	///<summary> Is it passable when approaching from outside? </summary>
	public virtual bool IsAtmosPassable(Vector3Int enteringFrom, bool isServer)
	{
		return true;
	}

	//This makes it so electrical Stuff can be done on its own thread
	public void SetElectricalData(ElectricalOIinheritance inElectricalData)
	{
		//Logger.Log("seting " + this.name);
		electricalData = inElectricalData;
	}

	//This makes it so electrical Stuff can be done on its own thread
	public void SetPipeData(PipeData InPipeData)
	{
		//Logger.Log("seting " + this.name);
		pipeData = InPipeData;
	}

	/// <summary>
	/// Logs a message to Matrix logging category only if
	/// matrixDebugLogging is turned on.
	/// </summary>
	/// <param name="log"></param>
	private void LogMatrixDebug(string log)
	{
		if (matrixDebugLogging)
		{
			Loggy.Log(log, Category.Matrix);
		}
	}

	/// <summary>
	/// Efficient fire exposure for all IFireExposable components on this register tile.
	/// Uses cached IFireExposable so no GC caused by GetComponent
	/// </summary>
	/// <param name="exposure"></param>
	public void OnExposed(FireExposure exposure)
	{
		if (fireExposables == null) return;
		foreach (var fireExposable in fireExposables)
		{
			fireExposable.OnExposed(exposure);
		}
	}

	public void SetNewSortingOrder(int newLayerId)
	{
		if (Manager3D.Is3D) return;
		if (CurrentsortingGroup == null) return;
		CurrentsortingGroup.sortingOrder = newLayerId;
	}

	public void SetNewSortingLayer(int newLayerId, bool BoolReorderSorting = true)
	{
		if (Manager3D.Is3D) return;
		CurrentsortingGroup.sortingLayerID = newLayerId;
		if (BoolReorderSorting)
		{
			ReorderSorting();
		}
	}

	private void ReorderSorting()
	{
		objectLayer.ClientObjects.ReorderObjects(LocalPositionClient);
		if(CustomNetworkManager.IsServer == false) return;
		objectLayer.ServerObjects.ReorderObjects(LocalPositionServer);
	}
}
