using UnityEngine;

namespace Container.HitConditions
{
	public abstract class HitInteractTileCondition : ScriptableObject
	{
		public abstract bool CheckCondition(
			RaycastHit2D hit,
			InteractableTiles interactableTiles,
			Vector3 worldPosition);
	}
}