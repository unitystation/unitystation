using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
	[ExecuteInEditMode]
	public abstract class RegisterTile : MonoBehaviour
	{
		public bool IsRegister { get; private set; }
		
		protected ObjectLayer layer;
		
		private Vector3Int _position;

		public Vector3Int Position
		{
			get { return _position; }
			protected set
			{
				OnAddTile(value);
				
				layer?.Objects.Remove(_position, this);
				layer?.Objects.Add(value, this);
				_position = value;
			}
		}

		public void Start()
		{			
			layer = transform.parent.GetComponent<ObjectLayer>();

			Register();
		}

		private void OnEnable()
		{
			// In case of recompilation and Start doesn't get called
			layer?.Objects.Add(Position, this);
			IsRegister = true;
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
			Position = Vector3Int.FloorToInt(transform.localPosition);
		}
		
		public void Register()
		{
			UpdatePosition();
			IsRegister = true;
		}
        
		public void Unregister()
		{
			layer.Objects.Remove(Position, this);
			IsRegister = false;
		}

		protected virtual void OnAddTile(Vector3Int newPosition)
		{
			
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
