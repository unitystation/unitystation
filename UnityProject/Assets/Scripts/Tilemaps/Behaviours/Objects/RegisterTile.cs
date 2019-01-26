using UnityEngine;
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
/// </summary>
[ExecuteInEditMode]
public abstract class RegisterTile : NetworkBehaviour
{
	private Vector3Int position;

	private ObjectLayer layer;

	public ObjectType ObjectType;

	/// <summary>
	/// Matrix this object lives in
	/// </summary>
	public Matrix Matrix { get; private set; }

	// Note that syncvar only runs on the client, so server must ensure SetParent
	// is invoked
	[SyncVar(hook = nameof(SetParent))] private NetworkInstanceId parentNetId;

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
	}

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

	public virtual bool IsAtmosPassable()
	{
		return true;
	}
}