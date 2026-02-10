using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours;

namespace US13.Projectiles.Behaviours
{
	public interface IOnHitInteractTile
	{
		bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition);
	}
}