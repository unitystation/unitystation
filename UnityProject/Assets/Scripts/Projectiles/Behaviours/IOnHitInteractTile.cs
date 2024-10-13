using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public interface IOnHitInteractTile
	{
		bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition);
	}
}