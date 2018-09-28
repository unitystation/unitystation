using UnityEngine;
using UnityEngine.Networking;

public enum ObjectType
{
	Item,
	Object,
	Player,
	Wire
}

[ExecuteInEditMode]
public abstract class RegisterTile : NetworkBehaviour
{
	private Vector3Int position;

	private ObjectLayer layer;

	public ObjectType ObjectType;

	public Matrix Matrix { get; private set; }

	[SyncVar(hook = nameof(SetParent))] private NetworkInstanceId parentNetId;

	public NetworkInstanceId ParentNetId
	{
		get { return parentNetId; }
		set { parentNetId = value; }
	}

	private void SetParent(NetworkInstanceId netId)
	{
		GameObject parent = ClientScene.FindLocalObject(netId);
		if (parent == null)
		{
			//nothing found
			return;
		}

		Unregister();
		layer = parent.GetComponentInChildren<ObjectLayer>();
		Matrix = parent.GetComponentInChildren<Matrix>();
		transform.parent = layer.transform;
		UpdatePosition();
	}

	public Vector3Int WorldPosition => MatrixManager.Instance.LocalToWorldInt(position, Matrix);

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

	public virtual bool IsPassable(Vector3Int from)
	{
		return true;
	}

	public virtual bool IsAtmosPassable()
	{
		return true;
	}
}