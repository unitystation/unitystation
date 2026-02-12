using UnityEngine;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours;

namespace US13.ScriptableObjects.Gun.HitConditions.Tile
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