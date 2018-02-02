using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterDoor : RegisterObject
	{
		public bool IsClosed = true;
		public bool OneDirectionRestricted;

		public override bool IsPassable(Vector3Int to)
		{
			if (IsClosed && OneDirectionRestricted)
			{
				Vector3Int v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

				return !(to - Position).Equals(v);
			}

			return !IsClosed;
		}

		public override bool IsPassable()
		{
			return !IsClosed;
		}

		public override bool IsAtmosPassable()
		{
			return !IsClosed;
		}
	}
}