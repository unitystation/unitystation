using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public interface IDespawn
	{
		/// <summary>
		/// Used for despawning
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="point"> End coordinate if nothing was hit </param>
		void Despawn(RaycastHit2D hit, Vector2 point);
	}
}