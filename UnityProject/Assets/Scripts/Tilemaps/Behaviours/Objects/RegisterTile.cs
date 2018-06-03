using System.Net.Configuration;
using PlayGroup;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using UnityEngine.Networking;
using Debug = System.Diagnostics.Debug;

namespace Tilemaps.Behaviours.Objects
{
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
				layer?.Objects.Remove(position, this);
				layer?.Objects.Add(value, this);
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

		private void OnEnable()
		{
			if (transform.parent != null)
			{
				layer = transform.parent.GetComponentInParent<ObjectLayer>();
				Matrix = transform.parent.GetComponentInParent<Matrix>();
				UpdatePosition();
			}

			// In case of recompilation and Start doesn't get called
			layer?.Objects.Add(Position, this);
		}

		private void OnDisable()
		{
			Unregister();
		}

		public void OnDestroy()
		{
			layer?.Objects.Remove(Position, this);
		}

		public void UpdatePosition()
		{
			Position = Vector3Int.RoundToInt(transform.localPosition);
		}

		public void Unregister() {
			Position = TransformState.HiddenPos;
			layer?.Objects.Remove(Position, this);
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
}