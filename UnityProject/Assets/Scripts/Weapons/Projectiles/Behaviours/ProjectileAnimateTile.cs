using System.Collections;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Creates an animated tile at a point of collision or end
	/// After animation, applies old tile effect
	/// </summary>
	public class ProjectileAnimateTile : MonoBehaviour, IOnDespawn
	{
		[SerializeField] private AnimatedTile animatedTile = null;

		[Tooltip("Living time of animated tile.")]
		[SerializeField] private float animationTime = 0;

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			if (hit.collider == null)
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

			interactableTiles.CreateAnimatedTile(position, animatedTile, animationTime);
		}

		private void OnCollision(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var interactableTiles = coll.GetComponentInParent<InteractableTiles>();

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			interactableTiles.CreateAnimatedTile(bulletHitTarget, animatedTile, animationTime);
		}

	}
}