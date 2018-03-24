using PlayGroup;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Objects
{
	public enum ObjectType
	{
		Item,
		Object,
		Player
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
			if(parent == null){
				//nothing found
				return;
			}
			Unregister();
			layer = parent.GetComponentInChildren<ObjectLayer>();
			Matrix = parent.GetComponent<Matrix>();
			transform.parent = layer.transform; 
			Register();
		}

		//Is not network synced, is used for prediction on local client when walking between matricies
		public void SetParentOnLocal(Transform newParent){
			Unregister();
			layer = newParent.GetComponentInChildren<ObjectLayer>();
			Matrix = newParent.GetComponent<Matrix>();
			transform.parent = layer.transform;
			Register();
		}

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
			if (transform.parent != null) {
				layer = transform.parent.GetComponentInParent<ObjectLayer>();
				Matrix = transform.parent.GetComponentInParent<Matrix>();
				Register();
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

		public void Register()
		{
			UpdatePosition();
		}

		public void Unregister()
		{
			layer?.Objects.Remove(Position, this);
		}

		public virtual bool IsPassable()
		{
			return true;
		}

		public virtual bool IsPassable(Vector3Int to)
		{
			return true;
		}

		public virtual bool IsAtmosPassable()
		{
			return true;
		}
	}
}