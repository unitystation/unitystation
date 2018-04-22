using Tilemaps.Behaviours.Meta;
using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterDoor : RegisterTile
	{
		private SystemManager systemManager;
		
		public bool OneDirectionRestricted;

		private void Awake()
		{
			systemManager = GetComponentInParent<SystemManager>();
		}

		[SerializeField]
		private bool isClosed = true;

		public bool IsClosed
		{
			get { return isClosed; }
			set
			{
				if (isClosed != value)
				{
					isClosed = value;
					systemManager.UpdateAt(Position);
				}
			}
		}

		public override bool IsPassable(Vector3Int to)
		{
			if (isClosed && OneDirectionRestricted)
			{
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				return !(to - Position).Equals(v);
			}

			return !isClosed;
		}

		public override bool IsPassable()
		{
			return !isClosed;
		}

		public override bool IsAtmosPassable()
		{
			return !isClosed;
		}
	}
}