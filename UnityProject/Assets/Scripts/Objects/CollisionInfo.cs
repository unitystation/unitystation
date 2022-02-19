using UnityEngine;

namespace Objects
{
	/// <summary>
	/// Collision information for objects (atmos tanks, lockers, players) that hit something with high speed
	/// </summary>
	public struct CollisionInfo
	{
		public float Speed;
		public Size Size;
		public Vector3Int CollisionTile;
		public float Damage => Mathf.Clamp(Speed * ((int)Size / 2f) - PushPull.HIGH_SPEED_COLLISION_THRESHOLD, 0f, 500f);
	}
}
