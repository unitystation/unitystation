using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	public abstract class HitInteractTileCondition : ScriptableObject
	{
		public abstract bool CheckCondition(
			RaycastHit2D hit,
			InteractableTiles interactableTiles,
			Vector3 worldPosition);
	}
}