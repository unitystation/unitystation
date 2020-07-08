using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Mines mineable walls on collision
	/// </summary>
	public class ProjectileMine : MonoBehaviour, IOnHit
	{
		public bool OnHit(RaycastHit2D hit)
		{
			return ProcessHit(hit);
		}

		protected virtual bool ProcessHit(RaycastHit2D hit)
		{
			var interactableTile = hit.collider.GetComponentInParent<InteractableTiles>();

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			return interactableTile.TryMine(bulletHitTarget);
		}
	}
}