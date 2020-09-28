using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public interface IOnHitInteractTile
	{
		bool Interact(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition);
	}
}