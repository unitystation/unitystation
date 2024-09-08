using ScriptableObjects.Gun.TileAnimations;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Creates an animated tile at a point of collision or end
	/// After animation, applies old tile effect
	/// </summary>
	public class ProjectileAnimateTile : MonoBehaviour, IOnDespawn
	{
		[SerializeField] private AnimationTile animationTile = null;

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			if (hit.CollisionHit.GameObject == null)
			{
				OnBeamEnd(point);
			}
			else
			{
				OnCollision(hit);
			}
		}

		private void OnBeamEnd(Vector2 position)
		{
			var interactableTiles = GetComponentInParent<InteractableTiles>();

			interactableTiles.CreateAnimatedTile(position, animationTile.Tile, animationTile.Time);
		}

		private void OnCollision(MatrixManager.CustomPhysicsHit hit)
		{
			var coll = hit.CollisionHit.GameObject;
			var interactableTiles = coll.GetComponentInParent<InteractableTiles>();

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.HitWorld.x - 0.01f * hit.Normal.x;
			bulletHitTarget.y = hit.HitWorld.y - 0.01f * hit.Normal.y;

			interactableTiles.CreateAnimatedTile(bulletHitTarget, animationTile.Tile, animationTile.Time);
		}

	}
}
