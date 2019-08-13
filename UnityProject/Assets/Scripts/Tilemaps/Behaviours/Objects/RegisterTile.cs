﻿using UnityEngine;
 using UnityEngine.Events;
 using UnityEngine.Networking;

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
public abstract class RegisterTile : NetworkBehaviour
{
	private bool hasInit = false;

	public ObjectLayer layer;

	public ObjectType ObjectType;

	private PushPull customTransform;
	public PushPull CustomTransform => customTransform ? customTransform : (customTransform = GetComponent<PushPull>());


	/// <summary>
	/// Invoked when parent matrix is going to change, just before the matrix is actually changed. Passes
	/// the new matrix.
	/// </summary>
	public MatrixChangeEvent OnMatrixWillChange = new MatrixChangeEvent();

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
				OnMatrixWillChange.Invoke(value);
				matrix = value;
			}
		}
	}
	private Matrix matrix;

	public NetworkInstanceId ParentNetId
	{
		get { return parentNetId; }
		set
		{
			// update parent if it changed
			if (value != parentNetId)
			{
				parentNetId = value;
				SetParent(parentNetId);
			}
		}
	}
	// Note that syncvar only runs on the client, so server must ensure SetParent
	// is invoked.
	[SyncVar(hook = nameof(SetParent))] private NetworkInstanceId parentNetId;

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
		protected set
		{
			if (layer)
			{
				layer.ServerObjects.Remove(serverLocalPosition, this);
				if ( value != TransformState.HiddenPos )
				{
					layer.ServerObjects.Add(value, this);
				}
			}

			serverLocalPosition = value;

		}
	}
	private Vector3Int serverLocalPosition;
	public Vector3Int LocalPositionClient
	{
		get => clientLocalPosition;
		protected set
		{
			if (layer)
			{
				layer.ClientObjects.Remove(clientLocalPosition, this);
				if ( value != TransformState.HiddenPos )
				{
					layer.ClientObjects.Add( value, this );
				}
			}

			clientLocalPosition = value;

		}
	}
	private Vector3Int clientLocalPosition;

	/// <summary>
	/// Invoked when parentNetId is changed on the server, updating the client's parentNetId. This
	/// applies the change by moving this object to live in the same objectlayer and matrix as that
	/// of the new parentid.
	/// provided netId
	/// </summary>
	/// <param name="netId">NetworkInstanceId of the new parent</param>
	private void SetParent(NetworkInstanceId netId)
	{
		GameObject parent = ClientScene.FindLocalObject(netId);
		if (parent == null)
		{
			//nothing found
			return;
		}

		//remove from current parent layer
		layer?.ClientObjects.Remove(LocalPositionClient, this);
		layer?.ServerObjects.Remove(LocalPositionServer, this);
		layer = parent.GetComponentInChildren<ObjectLayer>();
		Matrix = parent.GetComponentInChildren<Matrix>();
		transform.parent = layer.transform;
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

	public override void OnStartClient()
	{
		if (!parentNetId.IsEmpty())
		{
			SetParent(parentNetId);
		}
	}

	public override void OnStartServer()
	{
		if (transform.parent != null)
		{
			ParentNetId = transform.parent.GetComponentInParent<NetworkIdentity>().netId;
		}

		base.OnStartServer();
	}

	public void OnDestroy()
	{
		if (layer)
		{
			layer.ServerObjects.Remove(LocalPositionServer, this);
			layer.ClientObjects.Remove(LocalPositionClient, this);
		}
	}

	private void OnEnable()
	{
		ForceRegister();
	}

	[ContextMenu("Force Register")]
	private void ForceRegister()
	{
		if (transform.parent != null)
		{
			layer = transform.parent.GetComponentInParent<ObjectLayer>();
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

	private void OnDisable()
	{
		UnregisterClient();
		UnregisterServer();
	}

	public virtual void UpdatePositionServer()
	{
		LocalPositionServer = CustomTransform ? CustomTransform.Pushable.ServerLocalPosition : transform.localPosition.RoundToInt();
	}

	public virtual void UpdatePositionClient()
	{
		LocalPositionClient = CustomTransform ? CustomTransform.Pushable.ClientLocalPosition : transform.localPosition.RoundToInt();
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