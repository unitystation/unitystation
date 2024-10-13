using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public interface IOnDespawn
	{
		/// <summary>
		/// Interface for notifying components that
		/// game object is about to be despawned
		/// </summary>
		/// /// <param name="hit"> Collider responsible for despawn, new RaycastHit if nothing was hit </param>
		/// <param name="point"> Coordinate where object is about to despawn </param>
		void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point);
	}
}