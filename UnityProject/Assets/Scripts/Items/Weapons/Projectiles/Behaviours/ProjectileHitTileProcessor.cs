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

		public bool OnHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (hit.CollisionHit.TileLocation == null) return false;
			var interactableTile = hit.CollisionHit.TileLocation.metaTileMap.GetComponentInParent<InteractableTiles>();

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
		private bool TryProcessBehaviours(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTile, Vector3 bulletHitTarget)
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
		private Vector3 GetHitTileWorldPosition(MatrixManager.CustomPhysicsHit hit)
		{
			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.TileHitWorld.x;
			bulletHitTarget.y = hit.TileHitWorld.y;
			return bulletHitTarget;
		}
	}
}