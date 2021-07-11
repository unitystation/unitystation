using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Called every time projectile moved
	/// Basically allows to have behaviors
	/// which are called when projectile moves.
	/// </summary>
	public interface IOnMove
	{
		/// <summary>
		/// Called every update. Return true to stop the projectile.
		/// </summary>
		/// <param name="traveledDistance"></param>
		/// <param name="previousWorldPosition"></param>
		/// <returns> Used in bullet to request despawn </returns>
		bool OnMove(Vector2 traveledDistance, Vector2 previousWorldPosition);
	}
}
