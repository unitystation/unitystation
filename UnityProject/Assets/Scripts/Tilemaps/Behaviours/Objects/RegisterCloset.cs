using UnityEngine;

namespace Tilemaps.Behaviours.Objects
{
	[ExecuteInEditMode]
	public class RegisterCloset : RegisterObject
	{
		public bool IsClosed = true;

		public override bool IsPassable()
		{
			return !IsClosed;
		}
	}
}