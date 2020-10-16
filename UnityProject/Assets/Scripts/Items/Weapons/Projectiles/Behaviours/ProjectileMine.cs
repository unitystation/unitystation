using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Mines mineable walls on collision
	/// </summary>
	public class ProjectileMine : MonoBehaviour, IOnHitInteractTile
	{
		public virtual bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			return interactableTiles.TryMine(worldPosition);
		}
	}
}