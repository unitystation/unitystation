using Core;
using UnityEngine;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;

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
		public float Damage => Mathf.Clamp(Speed * ((int)Size / 2f) - UniversalObjectPhysics.HIGH_SPEED_COLLISION_THRESHOLD, 0f, 500f);
	}
}
