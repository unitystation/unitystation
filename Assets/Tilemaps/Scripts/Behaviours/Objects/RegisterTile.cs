using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
	[ExecuteInEditMode]
	public abstract class RegisterTile : MonoBehaviour
	{
		protected ObjectLayer layer;
		
		private Vector3Int _position;

		protected Vector3Int position
		{
			get { return _position; }
			set
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

			position = Vector3Int.FloorToInt(transform.localPosition);
		}

		private void OnEnable()
		{
			// In case of recompilation and Start doesn't get called
			layer?.Objects.Add(position, this);
		}

		public void OnDestroy()
		{
			layer?.Objects.Remove(position, this);
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
