using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileHitTileProcessor : MonoBehaviour, IOnHit
	{
		private IOnHitInteractTile[] behavioursInteractTile;

		private void Awake()
		{
			behavioursInteractTile = GetComponents<IOnHitInteractTile>();
		}

		public bool OnHit(RaycastHit2D hit)
		{
			var interactableTile = hit.collider.GetComponentInParent<InteractableTiles>();

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			bool isAnyProcessed = false;
			foreach (var behaviour in behavioursInteractTile)
			{
				if (behaviour.Interact(hit, interactableTile, bulletHitTarget))
				{
					isAnyProcessed = true;
				}
			}

			return isAnyProcessed;
		}
	}
}