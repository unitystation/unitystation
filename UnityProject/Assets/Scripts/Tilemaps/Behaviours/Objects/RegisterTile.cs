﻿using System;
 using System.Collections.Generic;
 using UnityEngine;
 using UnityEngine.Events;
 using Mirror;
 using UnityEngine.Serialization;

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
	private bool hasInit;


	private ObjectLayer objectLayer;
	/// <summary>
	/// Object layer this gameobject is in (all registertiles live in an object layer).
	/// </summary>
	public ObjectLayer ObjectObjectLayer => objectLayer;

	[Tooltip("The kind of object this is.")]
	[FormerlySerializedAs("ObjectType")] [SerializeField]
	private ObjectType objectType;
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
				matrix = value;
			}
		}
	}
	private Matrix matrix;
	public bool MatrixIsMovable => Matrix && Matrix.MatrixMove;


	[SyncVar(hook = nameof(SyncParentMatrixNetId))]
	private uint parentMatrixNetId;
	/// <summary>
	/// NetId of our current parent matrix.
	/// </summary>
	protected uint ParentMatrixNetId => parentMatrixNetId;

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
			if (objectLayer)
			{
				objectLayer.ClientObjects.Remove(clientLocalPosition, this);
				if ( value != TransformState.HiddenPos )
				{
					objectLayer.ClientObjects.Add( value, this );
				}
			}

			clientLocalPosition = value;

		}
	}
	private Vector3Int clientLocalPosition;

	/// <summary>
	/// Event invoked on server side when position changes. Passes the new local position in the matrix.
	/// </summary>
	[NonSerialized]
	public readonly Vector3IntEvent OnLocalPositionChangedServer = new Vector3IntEvent();

	//we have lifecycle methods from lifecycle system, but lots of things currently depend on this register tile
	//being initialized as early as possible so we still have this in place.
	private void OnEnable()
	{
		ForceRegister();
	}

	public override void OnStartClient()
	{
		SyncParentMatrixNetId(parentMatrixNetId);
	}

	public void OnDestroy()
	{
		if (objectLayer)
		{
			objectLayer.ServerObjects.Remove(LocalPositionServer, this);
			objectLayer.ClientObjects.Remove(LocalPositionClient, this);
		}
	}

	public void OnDespawnServer(DespawnInfo info)
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
	}



	/// <summary>
	/// Set our parent matrix net ID to this.
	/// </summary>
	/// <param name="newParentMatrixNetID"></param>
	[Server]
	public void ServerSetParentMatrixNetID(uint newParentMatrixNetID)
	{
		SyncParentMatrixNetId(newParentMatrixNetID);
	}

	/// <summary>
	/// Invoked when parentNetId is changed on the server, updating the client's parentNetId. This
	/// applies the change by moving this object to live in the same objectlayer and matrix as that
	/// of the new parentid.
	/// provided netId
	/// </summary>
	/// <param name="newParentMatrixNetID">uint of the new parent</param>
	private void SyncParentMatrixNetId(uint newParentMatrixNetID)
	{
		if (this.parentMatrixNetId == newParentMatrixNetID) return;
		if (newParentMatrixNetID == NetId.Invalid) return;

		this.parentMatrixNetId = newParentMatrixNetID;
		NetworkIdentity.spawned.TryGetValue(newParentMatrixNetID, out var parentMatrix);
		if (parentMatrix == null)
		{
			//nothing found
			return;
		}

		//remove from current parent layer
		objectLayer?.ClientObjects.Remove(LocalPositionClient, this);
		objectLayer?.ServerObjects.Remove(LocalPositionServer, this);
		objectLayer = parentMatrix.GetComponentInChildren<ObjectLayer>();
		Matrix = parentMatrix.GetComponentInChildren<Matrix>();
		transform.SetParent( objectLayer.transform, true );
		//if we are hidden, remain hidden, otherwise update because we have a new parent
		if (LocalPositionClient != TransformState.HiddenPos)
		{
			UpdatePositionClient();
		}
		if (LocalPositionServer != TransformState.HiddenPos)
		{
			UpdatePositionServer();
		}
		OnParentChangeComplete();

		if (!hasInit)
		{
			hasInit = true;
		}

	}

	[ContextMenu("Force Register")]
	private void ForceRegister()
	{
		if (transform.parent != null)
		{
			objectLayer = transform.parent.GetComponentInParent<ObjectLayer>();
			Matrix = transform.parent.GetComponentInParent<Matrix>();

			LocalPositionServer = Vector3Int.RoundToInt(transform.localPosition);
			LocalPositionClient = Vector3Int.RoundToInt(transform.localPosition);
		}
	}

	public void UnregisterClient()
	{
		LocalPositionClient = TransformState.HiddenPos;
	}
	public void UnregisterServer()
	{
		LocalPositionServer = TransformState.HiddenPos;
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
	}

	public virtual void UpdatePositionClient()
	{
		LocalPositionClient = CustomTransform ? CustomTransform.Pushable.ClientLocalPosition : transform.localPosition.RoundToInt();
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
			UpdateManager.Instance.Add(UpdatePollCrossMatrixRelationships);
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
			UpdateManager.Instance.Remove(UpdatePollCrossMatrixRelationships);
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

	/// <summary>
	/// Invoked when the parent net ID of this RegisterTile has changed, after reparenting
	/// has been performed in RegisterTile (which updates the parent net ID, parent transform, parent
	/// matrix, position, object layer, and parent matrix move if one is present in the matrix).
	/// Allows subclasses to respond to this event and do additional processing
	/// based on the new parent net ID.
	/// </summary>
	protected virtual void OnParentChangeComplete()
	{
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

}

 /// <summary>
 /// Event fired when current matrix is changing. Passes the new matrix.
 /// </summary>
 public class MatrixChangeEvent : UnityEvent<Matrix>{};