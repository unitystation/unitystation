using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Mirror;

public enum ObjectType
{
	Item,
	Object,
	Player,
	Wire
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
	private bool initialized;

	[Tooltip("For debug purposes only. Logs trace-level debug messages in " +
	         "Matrix logging category for this particular" +
	         " object so it's easier to see what's happening to it.")]
	public bool matrixDebugLogging;

	private ObjectLayer objectLayer;
	/// <summary>
	/// Object layer this gameobject is in (all registertiles live in an object layer).
	/// </summary>
	public ObjectLayer ObjectObjectLayer => objectLayer;

	/// <summary>
	/// Tile change manager of the matrix this object is on.
	/// </summary>
	public TileChangeManager TileChangeManager => Matrix ? Matrix.TileChangeManager : null;

	[Tooltip("The kind of object this is.")]
	[FormerlySerializedAs("ObjectType")] [SerializeField]
	private ObjectType objectType = ObjectType.Item;
	/// <summary>
	/// The kind of object this is.
	/// </summary>
	public ObjectType ObjectType => objectType;

	private PushPull customTransform;
	public PushPull CustomTransform => customTransform ? customTransform : (customTransform = GetComponent<PushPull>());

	/// <summary>
	/// Matrix this object lives in
	/// </summary>
	public Matrix Matrix
	{
		get => matrix;
		private set
		{
			if (value)
			{
				//LogMatrixDebug($"Matrix set from {matrix} to {value}");
				if (matrix != null && matrix.MatrixMove != null)
				{
					matrix.MatrixMove.MatrixMoveEvents.OnRotate.RemoveListener(OnRotate);
				}

				matrix = value;
				if (matrix != null && matrix.MatrixMove != null)
				{
					//LogMatrixDebug($"Registered OnRotate to {matrix}");
					matrix.MatrixMove.MatrixMoveEvents.OnRotate.AddListener(OnRotate);
					if (isServer)
					{
						OnRotate(new MatrixRotationInfo(matrix.MatrixMove, matrix.MatrixMove.FacingOffsetFromInitial, NetworkSide.Server, RotationEvent.Register));
					}
					OnRotate(new MatrixRotationInfo(matrix.MatrixMove, matrix.MatrixMove.FacingOffsetFromInitial, NetworkSide.Client, RotationEvent.Register));
				}
			}

		}
	}


	private Matrix matrix;
	public bool MatrixIsMovable => Matrix && Matrix.MatrixMove;

	/// <summary>
	/// Invoked when the parent net ID of this RegisterTile has changed, after reparenting
	/// has been performed in RegisterTile (which updates the parent net ID, parent transform, parent
	/// matrix, position, object layer, and parent matrix move if one is present in the matrix).
	/// Allows components to respond to this event and do additional processing
	/// based on the new parent net ID.
	/// </summary>
	[NonSerialized]
	public UnityEvent OnParentChangeComplete = new UnityEvent();

	/// <summary>
	/// Invoked clientside when object is in the world (not at hidden pos) and then disappears for whatever reason
	/// (registered to hidden pos)
	/// </summary>
	[NonSerialized]
	public UnityEvent OnDisappearClient = new UnityEvent();
	/// <summary>
	/// Invoked clientside when object is invisible to client (at hidden pos) and then becomes visible
	/// (not at hidden pos)
	/// </summary>
	[NonSerialized]
	public UnityEvent OnAppearClient = new UnityEvent();

	/// <summary>
	/// Invoked serverside when object is despawned
	/// </summary>
	[NonSerialized]
	public UnityEvent OnDespawnedServer = new UnityEvent();


	[SyncVar(hook = nameof(SyncNetworkedMatrixNetId))]
	private uint networkedMatrixNetId;
	/// <summary>
	/// NetId of our current networked matrix. Note this id is on the parent of the
	/// Matrix gameObject, i.e. the one with NetworkedMatrix not the one with Matrix, hence calling
	/// it "networked matrix".
	/// </summary>
	protected uint NetworkedMatrixNetId => networkedMatrixNetId;

	/// <summary>
	/// Returns the correct client/server version of world position depending on if this is
	/// called on client or server.
	/// </summary>
	public Vector3Int WorldPosition => isServer ? WorldPositionServer : WorldPositionClient;

	public Vector3Int WorldPositionServer => MatrixManager.Instance.LocalToWorldInt(serverLocalPosition, Matrix);
	public Vector3Int WorldPositionClient => MatrixManager.Instance.LocalToWorldInt(clientLocalPosition, Matrix);

	/// <summary>
	/// Registered local position of this object. Returns correct value depending on if this is on the
	/// server or client.
	/// </summary>
	public Vector3Int LocalPosition => isServer ? LocalPositionServer : LocalPositionClient;

	/// <summary>
	/// the "registered" local position of this object (which might differ from transform.localPosition).
	/// It will be set to TransformState.HiddenPos when hiding the object.
	/// </summary>
	public Vector3Int LocalPositionServer
	{
		get => serverLocalPosition;
		private set
		{
			if (objectLayer)
			{
				objectLayer.ServerObjects.Remove(serverLocalPosition, this);
				if ( value != TransformState.HiddenPos )
				{
					objectLayer.ServerObjects.Add(value, this);
				}
			}

			serverLocalPosition = value;

		}
	}
	private Vector3Int serverLocalPosition;
	public Vector3Int LocalPositionClient
	{
		get => clientLocalPosition;
		private set
		{
			bool appeared = clientLocalPosition == TransformState.HiddenPos && value != TransformState.HiddenPos;
			bool disappeared = clientLocalPosition != TransformState.HiddenPos && value == TransformState.HiddenPos;
			if (objectLayer)
			{
				objectLayer.ClientObjects.Remove(clientLocalPosition, this);
				if ( value != TransformState.HiddenPos )
				{
					objectLayer.ClientObjects.Add( value, this );
				}
			}

			clientLocalPosition = value;

			if (appeared)
			{
				OnAppearClient.Invoke();
			}
			if (disappeared)
			{
				OnDisappearClient.Invoke();
			}

		}
	}
	private Vector3Int clientLocalPosition;

	/// <summary>
	/// Event invoked on server side when position changes. Passes the new local position in the matrix.
	/// </summary>
	[NonSerialized]
	public readonly Vector3IntEvent OnLocalPositionChangedServer = new Vector3IntEvent();

	private IMatrixRotation[] matrixRotationHooks;
	private CustomNetTransform cnt;
	//cached for fast fire exposure without gc
	private IFireExposable[] fireExposables;
	private bool hasCachedComponents = false;


	private ElectricalOIinheritance electricalData;
	public ElectricalOIinheritance ElectricalData => electricalData;

	private Pipes.PipeData pipeData;
	public Pipes.PipeData PipeData => pipeData;

	protected virtual void Awake()
	{
		EnsureInit();
	}

	private void EnsureInit()
	{
		if (hasCachedComponents) return;
		cnt = GetComponent<CustomNetTransform>();
		matrixRotationHooks = GetComponents<IMatrixRotation>();
		fireExposables = GetComponents<IFireExposable>();
	}


	//we have lifecycle methods from lifecycle system, but lots of things currently depend on this register tile
	//being initialized as early as possible so we still have this in place.
	private void OnEnable()
	{
		LogMatrixDebug("OnEnable");
		initialized = false;
		ForceRegister();
		EventManager.AddHandler(EVENT.MatrixManagerInit, MatrixManagerInit);
	}

	private void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.MatrixManagerInit, MatrixManagerInit);
	}

	public override void OnStartClient()
	{
		LogMatrixDebug("OnStartClient");
		EnsureInit();
		SyncNetworkedMatrixNetId(networkedMatrixNetId, networkedMatrixNetId);
	}

	public override void OnStartServer()
	{
		LogMatrixDebug("OnStartServer");
		EnsureInit();
		ForceRegister();
		if (Matrix != null)
		{
			networkedMatrixNetId = Matrix.transform.parent.gameObject.NetId();
		}
	}

	public void OnDestroy()
	{
		if (objectLayer)
		{
			objectLayer.ServerObjects.Remove(LocalPositionServer, this);
			objectLayer.ClientObjects.Remove(LocalPositionClient, this);
		}
	}

	public virtual void OnDespawnServer(DespawnInfo info)
	{
		//cancel all relationships
		if (sameMatrixRelationships != null)
		{
			foreach (var relationship in sameMatrixRelationships)
			{
				Logger.LogTraceFormat("Cancelling spatial relationship {0} because {1} is despawning.", Category.SpatialRelationship, relationship, this);
				SpatialRelationship.ServerEnd(relationship);
			}
		}
		if (crossMatrixRelationships != null)
		{
			foreach (var relationship in crossMatrixRelationships)
			{
				Logger.LogTraceFormat("Cancelling spatial relationship {0} because {1} is despawning.", Category.SpatialRelationship, relationship, this);
				SpatialRelationship.ServerEnd(relationship);
			}
		}

		OnDespawnedServer.Invoke();
	}

	//This makes it so electrical Stuff can be done on its own thread
	public void SetElectricalData(ElectricalOIinheritance inElectricalData)
	{
		//Logger.Log("seting " + this.name);
		electricalData = inElectricalData;
	}

	//This makes it so electrical Stuff can be done on its own thread
	public void SetPipeData(Pipes.PipeData InPipeData)
	{
		//Logger.Log("seting " + this.name);
		pipeData = InPipeData;
	}


	/// <summary>
	/// Set our parent matrix net ID to this.
	/// </summary>
	/// <param name="newNetworkedMatrixNetID"></param>
	[Server]
	public void ServerSetNetworkedMatrixNetID(uint newNetworkedMatrixNetID)
	{
		LogMatrixDebug("ServerSetNetworkedMatrixNetID");
		networkedMatrixNetId = newNetworkedMatrixNetID;
	}


	/// <summary>
	/// Invoked when parentNetId is changed on the server, updating the client's parentNetId. This
	/// applies the change by moving this object to live in the same objectlayer and matrix as that
	/// of the new parentid.
	/// provided netId
	/// </summary>
	/// <param name="oldNetworkMatrixId"></param>
	/// <param name="newNetworkedMatrixNetID">uint of the new parent</param>
	private void SyncNetworkedMatrixNetId(uint oldNetworkMatrixId, uint newNetworkedMatrixNetID)
	{
		//LogMatrixDebug($"Sync parent net id {networkedMatrixNetId}");
		EnsureInit();
		//note: previously we returned immediately if the new ID matched our current networkMatrixNetId,
		//but because Mirror actually sets our networkMatrixNetId for us prior to this hook being called
		//this would incorrectly skip the registration logic. This issue seems to only have
		//occurred after upgrading mirror to the Feb 04, 20202 release .
		//It's not really a performance concern since this sort of update happens rarely
		if (newNetworkedMatrixNetID == NetId.Invalid || newNetworkedMatrixNetID == NetId.Empty) return;

		this.networkedMatrixNetId = newNetworkedMatrixNetID;

		NetworkedMatrix.InvokeWhenInitialized(networkedMatrixNetId, FinishNetworkedMatrixRegistration);
	}

	private void FinishNetworkedMatrixRegistration(NetworkedMatrix networkedMatrix)
	{
		//if we had any spin rotation, preserve it,
		//otherwise all objects should always have upright local rotation
		var rotation = transform.rotation;
		//only CNTs can have spin rotation
		bool hadSpinRotation = cnt && Quaternion.Angle(transform.localRotation, Quaternion.identity) > 5;
		objectLayer?.ClientObjects.Remove(LocalPositionClient, this);
		objectLayer?.ServerObjects.Remove(LocalPositionServer, this);
		objectLayer = networkedMatrix.GetComponentInChildren<ObjectLayer>();
		transform.SetParent( objectLayer.transform, true );
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
		Matrix = networkedMatrix.GetComponentInChildren<Matrix>();


		//if we are hidden, remain hidden, otherwise update because we have a new parent
		if (LocalPositionClient != TransformState.HiddenPos)
		{
			UpdatePositionClient();
		}
		if (LocalPositionServer != TransformState.HiddenPos)
		{
			UpdatePositionServer();
		}
		OnParentChangeComplete.Invoke();

		if (!initialized)
		{
			initialized = true;
		}
	}

	[ContextMenu("Force Register")]
	private void ForceRegister()
	{
		//TODO: Not sure if this is okay, as it sets the Matrix but it doesn't go through
		//the full matrix init logic. It might be better to call FinishNetworkedMatrixRegistration after
		//setting the matrix, but that would need to be tested.
		LogMatrixDebug("ForceRegister");
		if (transform.parent != null)
		{
			objectLayer = transform.parent.GetComponentInParent<ObjectLayer>();
			Matrix = transform.parent.GetComponentInParent<Matrix>();

			LocalPositionServer = Vector3Int.RoundToInt(transform.localPosition);
			LocalPositionClient = Vector3Int.RoundToInt(transform.localPosition);
		}
	}

	private List<Action<MatrixInfo>> matrixManagerDependantActions = new List<Action<MatrixInfo>>();
	private bool listenerAdded = false;
	private MatrixInfo pendingInfo;

	/// <summary>
	/// If your start initialization relies on Matrix being
	/// initialized with the correct MatrixInfo then send the action here.
	/// It will wait until the matrix is properly configured
	/// before calling the action
	/// </summary>
	/// <param name="initAction">Action to call when the Matrix is configured</param>
	public void WaitForMatrixInit(Action<MatrixInfo> initAction)
	{
		matrixManagerDependantActions.Add(initAction);
		if (!matrix.MatrixInfoConfigured)
		{
			if (!listenerAdded)
			{
				listenerAdded = true;
				matrix.OnConfigLoaded += MatrixManagerInitAction;
			}
		}
		else
		{
			MatrixManagerInitAction(matrix.MatrixInfo);
		}
	}

	private void MatrixManagerInitAction(MatrixInfo matrixInfo)
	{
		if (!MatrixManager.IsInitialized)
		{
			pendingInfo = matrixInfo;
			return;
		}

		if (listenerAdded)
		{
			listenerAdded = false;
			matrix.OnConfigLoaded -= MatrixManagerInitAction;
		}

		foreach (var a in matrixManagerDependantActions)
		{
			a.Invoke(matrixInfo);
		}
		matrixManagerDependantActions.Clear();
	}

	void MatrixManagerInit()
	{
		MatrixManagerInitAction(pendingInfo);
	}

	public void UnregisterClient()
	{
		LocalPositionClient = TransformState.HiddenPos;
	}
	public void UnregisterServer()
	{
		LocalPositionServer = TransformState.HiddenPos;
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

	public virtual void UpdatePositionServer()
	{
		var prevPosition = LocalPositionServer;
		LocalPositionServer = CustomTransform ? CustomTransform.Pushable.ServerLocalPosition : transform.localPosition.RoundToInt();
		if (prevPosition != LocalPositionServer)
		{
			OnLocalPositionChangedServer.Invoke(LocalPositionServer);
			CheckSameMatrixRelationships();
		}
		//LogMatrixDebug($"Server position from {prevPosition} to {LocalPositionServer}");
	}

	public virtual void UpdatePositionClient()
	{

		var prevPosition = LocalPositionClient;
		LocalPositionClient = CustomTransform ? CustomTransform.Pushable.ClientLocalPosition : transform.localPosition.RoundToInt();
		//LogMatrixDebug($"Client position from {LocalPositionClient} to {prevPosition}");
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
		if (toAdd.Other(this).matrix != Matrix)
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
					Logger.LogTraceFormat("Cancelling spatial relationship {0} because OnRelationshipChanged" +
					                      " returned true.", Category.SpatialRelationship, cancelled);
					SpatialRelationship.ServerEnd(cancelled);
				}
			}

			if (toSwitch != null)
			{
				foreach (var switched in toSwitch)
				{
					Logger.LogTraceFormat("Switching spatial relationship {0} to cross matrix because" +
					                      " objects moved to different matrices.", Category.SpatialRelationship, switched);
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
		Logger.LogTraceFormat("Adding same matrix relationship {0} on {1}",
			Category.SpatialRelationship, toAdd, this);
		sameMatrixRelationships.Add(toAdd);
	}
	private void AddCrossMatrixRelationship(BaseSpatialRelationship toAdd)
	{
		//we only check cross matrix relationships if we are the leader, since only
		//one side needs to poll.
		if (!toAdd.IsLeader(this))
		{
			Logger.LogTraceFormat("Not adding cross matrix relationship {0} on {1} because {1} is not the leader",
				Category.SpatialRelationship, toAdd, this);
			return;
		}
		Logger.LogTraceFormat("Adding cross matrix relationship {0} on {1}",
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
		Logger.LogTraceFormat("Removing same matrix relationship {0} from {1}",
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
		Logger.LogTraceFormat("Removing cross matrix relationship {0} from {1}",
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
					Logger.LogTraceFormat("Cancelling spatial relationship {0} because OnRelationshipChanged" +
					                      " returned true.", Category.SpatialRelationship, cancelled);
					SpatialRelationship.ServerEnd(cancelled);
				}
			}

			if (toSwitch != null)
			{
				foreach (var switched in toSwitch)
				{
					Logger.LogTraceFormat("Switching spatial relationship {0} to same matrix because" +
					                      " objects moved to the same matrix.", Category.SpatialRelationship, switched);
					RemoveCrossMatrixRelationship(switched);
					AddSameMatrixRelationship(switched);
				}
			}
		}
	}

	public virtual bool IsPassable(bool isServer)
	{
		return true;
	}

	/// Is it passable when approaching from outside?
	public virtual bool IsPassable(Vector3Int from, bool isServer)
	{
		return true;
	}

	/// Is it passable when trying to leave it?
	public virtual bool IsPassableTo(Vector3Int to, bool isServer)
	{
		return true;
	}

	public virtual bool IsAtmosPassable(Vector3Int from, bool isServer)
	{
		return true;
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
			Logger.Log(log, Category.Matrix);
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
}

 /// <summary>
 /// Event fired when current matrix is changing. Passes the new matrix.
 /// </summary>
 public class MatrixChangeEvent : UnityEvent<Matrix>{};
