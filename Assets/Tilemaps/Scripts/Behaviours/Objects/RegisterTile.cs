using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
	[ExecuteInEditMode]
	public abstract class RegisterTile : MonoBehaviour
	{
		protected ObjectLayer layer;
		
		private Vector3Int _positon;

		protected Vector3Int position
		{
			get { return _positon; }
			set
			{
				if (!value.Equals(_positon))
				{
					OnAddTile(_positon, value);
					
					_positon = value;
				}
			}
		}

		public virtual void Start()
		{
			position = Vector3Int.FloorToInt(transform.localPosition);
			
			layer = transform.parent.GetComponent<ObjectLayer>();
			
			layer.Objects.Add(position, this);
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

		protected virtual void OnAddTile(Vector3Int oldPosition, Vector3Int newPosition)
		{
			layer?.Objects.Remove(oldPosition, this);
			layer?.Objects.Add(newPosition, this);
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
