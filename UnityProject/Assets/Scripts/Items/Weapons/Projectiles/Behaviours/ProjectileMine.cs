using UnityEngine;
using Tiles;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Mines mineable walls on collision if hardness of the projectile is equal to or exceeds the tile's hardness
	/// </summary>

	public class ProjectileMine : MonoBehaviour, IOnHitInteractTile
	{

		[Range(1, 10)]
		[Tooltip("what degree of hardness this projectile can overcome. Higher means the projectile can mine more types of things.")]
		[SerializeField] private int projectileHardness = 5;

		public virtual bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{

			var tile = interactableTiles.InteractableLayerTileAt(worldPosition, true);
			if (tile is BasicTile basicTile)
			{
				if (projectileHardness < basicTile.MiningHardness) return false;
			}
			return interactableTiles.TryMine(worldPosition);
		}

	}

}