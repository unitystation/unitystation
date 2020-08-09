using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Main processor for handling interactions which require InteractableTiles component
	/// </summary>
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

			var bulletHitTarget = GetHitTileWorldPosition(hit);

			return TryProcessBehaviours(hit, interactableTile, bulletHitTarget);
		}

		/// <summary>
		///  Invokes cached behaviours
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="interactableTile"></param>
		/// <param name="bulletHitTarget"></param>
		/// <returns> True if at least one behaviour returned true </returns>
		private bool TryProcessBehaviours(RaycastHit2D hit, InteractableTiles interactableTile, Vector3 bulletHitTarget)
		{
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

		/// <summary>
		/// It is necessary to off set hit position of a raycast
		/// If you won't do it, you will get wrong tile
		/// when shooting up or left side of the tile
		/// </summary>
		/// <param name="hit"></param>
		/// <returns></returns>
		private Vector3 GetHitTileWorldPosition(RaycastHit2D hit)
		{
			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;
			return bulletHitTarget;
		}
	}
}