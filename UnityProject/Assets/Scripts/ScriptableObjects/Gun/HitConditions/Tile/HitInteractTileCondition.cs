using UnityEngine;

namespace ScriptableObjects.Gun.HitConditions.Tile
{
	public abstract class HitInteractTileCondition : ScriptableObject
	{
		/// <summary>
		/// This is a test for injecting validation into scripts by using scriptable objects
		/// </summary>
		/// <param name="hit"></param>
		/// <param name="interactableTiles"></param>
		/// <param name="worldPosition"></param>
		/// <returns></returns>
		public abstract bool CheckCondition(
			MatrixManager.CustomPhysicsHit hit,
			InteractableTiles interactableTiles,
			Vector3 worldPosition);
	}
}