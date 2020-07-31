using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public interface IOnDespawn
	{
		/// <summary>
		/// Interface for notifying components that
		/// game object is about to be despawned
		/// </summary>
		/// /// <param name="hit"></param>
		/// <param name="point"> End coordinate if nothing was hit </param>
		void OnDespawn(RaycastHit2D hit, Vector2 point);
	}
}