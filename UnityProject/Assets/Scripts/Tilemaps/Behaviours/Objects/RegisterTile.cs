﻿using UnityEngine;
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
/// Also tracks the Matrix the object is in. Any object that needs to subscribe to rotation events should
/// do so via RegisterTile.OnRotateEnd / OnRotateStart rather than manually tracking / subscribing to the current matrix itself,
/// as RegisterTile takes care of tracking the current matrix.
/// </summary>
[ExecuteInEditMode]
public abstract class RegisterTile : NetworkBehaviour
{
	/// <summary>
	/// When true, registertiles will rotate to their new orientation at the end of matrix rotation. When false
	/// they will rotate to the new orientation at the start of matrix rotation.
	/// </summary>
	private const bool ROTATE_AT_END = true;

	/// <summary>
	/// Invoked when rotation ends. Passes through the OrientationEvent from this registertile's
	/// current MatrixMove. Allows objects which have a RegisterTile component to subscribe to rotation events
	/// for the correct matrix rather than having to determine which matrix to subscribe to and having to
	/// constantly check if the matrix has changed. In general, it's better to use this event rather than
	/// MatrixMove.OnRotateEnd because RegisterTile takes care of always subscribing to the correct matrix
	/// even when the matrix changes.
	/// </summary>
	[HideInInspector]
	public OrientationEvent OnRotateEnd = new OrientationEvent();
	/// <summary>
	/// See <see cref="OnRotateEnd"/>. Invoked when rotation begins.
	/// </summary>
	[HideInInspector]
	public OrientationEvent OnRotateStart = new OrientationEvent();

	private ObjectLayer layer;

	public ObjectType ObjectType;

	[Tooltip("If true, this object's sprites will rotate along with the matrix. If false, this wallmount's sprites" +
	         " will always remain upright (top pointing to the top of the screen")]
	public bool rotateWithMatrix;

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
				//update matrix move as well
				//if it exists
				MatrixMove = matrix.transform.root.GetComponent<MatrixMove>();
			}
		}
	}
	private Matrix matrix;

	/// <summary>
	/// MatrixMove this registerTile exists in, if the tile's current matrix can actually move.
	/// Null if there is no MatrixMove for this matrix (i.e. for an non-movable matrix).
	/// Cached so we don't have to re-locate it every time it's needed
	/// </summary>
	public MatrixMove MatrixMove
	{
		get => matrixMove;
		private set
		{
			//unsubscribe from old event
			if (matrixMove != null)
			{
				matrixMove.OnRotateStart.RemoveListener(OnRotationStart);
				matrixMove.OnRotateEnd.RemoveListener(OnRotationEnd);
			}

			matrixMove = value;
			//set up rotation listener
			if (matrixMove != null)
			{
				matrixMove.OnRotateStart.AddListener(OnRotationStart);
				matrixMove.OnRotateEnd.AddListener(OnRotationEnd);
			}
		}
	}
	private MatrixMove matrixMove;

	//cached spriteRenderers of this gameobject
	protected SpriteRenderer[] spriteRenderers;

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

	public Vector3Int WorldPosition => MatrixManager.Instance.LocalToWorldInt(position, Matrix);

	/// <summary>
	/// the "registered" local position of this object (which might differ from transform.localPosition).
	/// It will be set to TransformState.HiddenPos when hiding the object.
	/// </summary>
	public Vector3Int Position
	{
		get { return position; }
		private set
		{
			if (layer)
			{
				layer.Objects.Remove(position, this);
				layer.Objects.Add(value, this);
			}

			position = value;

		}
	}
	private Vector3Int position;

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
		layer?.Objects.Remove(Position, this);
		layer = parent.GetComponentInChildren<ObjectLayer>();
		Matrix = parent.GetComponentInChildren<Matrix>();
		transform.parent = layer.transform;
		//if we are hidden, remain hidden, otherwise update because we have a new parent
		if (Position != TransformState.HiddenPos)
		{
			UpdatePosition();
		}
		OnParentChangeComplete();
	}

	private void Start()
	{
		Init();
	}

	private void Awake()
	{
		//some things (such as items in closets) do not start with sprite renderers so we need to init
		//when they are awoken not just on Start
		Init();
	}

	private void Init()
	{
		//cache the sprite renderers
		if (spriteRenderers == null)
		{
			spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
			//orient upright
			if (!rotateWithMatrix && spriteRenderers != null)
			{
				foreach (SpriteRenderer renderer in spriteRenderers)
				{
					renderer.transform.rotation = Quaternion.identity;
				}
			}
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
			layer.Objects.Remove(Position, this);
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
			UpdatePosition();
		}
	}

	public void Unregister()
	{
		Position = TransformState.HiddenPos;

		if (layer)
		{
			layer.Objects.Remove(Position, this);
		}
	}

	private void OnDisable()
	{
		Unregister();
	}

	public void UpdatePosition()
	{
		Position = Vector3Int.RoundToInt(transform.localPosition);
	}

	/// <summary>
	/// Invoked when receiving rotation event from our current matrix's matrixmove
	/// </summary>
	/// <param name="fromCurrent">offset our matrix has rotated by from its previous orientation</param>
	protected virtual void OnRotationStart(RotationOffset fromCurrent, bool isInitialRotation)
	{
		if (!ROTATE_AT_END && spriteRenderers != null)
		{
			// reorient to stay upright if we are configured to do so
			if (!rotateWithMatrix)
			{
				foreach (SpriteRenderer renderer in spriteRenderers)
				{
					renderer.transform.rotation = Quaternion.identity;
				}
			}
		}

		OnRotateStart.Invoke(fromCurrent, isInitialRotation);
	}

	/// <summary>
	/// Invoked when receiving rotation event from our current matrix's matrixmove
	/// </summary>
	/// <param name="fromCurrent">offset our matrix has rotated by from its previous orientation</param>
	protected virtual void OnRotationEnd(RotationOffset fromCurrent, bool isInitialRotation)
	{
		if (ROTATE_AT_END && spriteRenderers != null)
		{
			// reorient to stay upright if we are configured to do so
			if (!rotateWithMatrix)
			{
				foreach (SpriteRenderer renderer in spriteRenderers)
				{
					renderer.transform.rotation = Quaternion.identity;
				}
			}
		}

		OnRotateEnd.Invoke(fromCurrent, isInitialRotation);
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

	public virtual bool IsPassable()
	{
		return true;
	}

	/// Is it passable when approaching from outside?
	public virtual bool IsPassable(Vector3Int from)
	{
		return true;
	}

	/// Is it passable when trying to leave it?
	public virtual bool IsPassableTo(Vector3Int to)
	{
		return true;
	}

	public virtual bool IsAtmosPassable(Vector3Int from)
	{
		return true;
	}
}