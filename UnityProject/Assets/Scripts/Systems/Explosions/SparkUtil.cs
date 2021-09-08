using ScriptableObjects;
using UnityEngine;

namespace Systems.Explosions
{
	public static class SparkUtil
	{
		/// <summary>
		/// Try spark using object's register tile
		/// </summary>
		/// <returns>Null if the spark failed, else the new spark object.</returns>
		public static GameObject TrySpark(ObjectBehaviour sourceObjectBehaviour, float chanceToSpark = 75, bool expose = true)
		{
			if (sourceObjectBehaviour == null) return null;

			//Clamp just in case
			chanceToSpark = Mathf.Clamp(chanceToSpark, 1, 100);

			//E.g will have 25% chance to not spark when chanceToSpark = 75
			if(DMMath.Prob(100 - chanceToSpark)) return null;

			var worldPos = sourceObjectBehaviour.AssumedWorldPositionServer();

			var result = Spawn.ServerPrefab(CommonPrefabs.Instance.SparkEffect, worldPos, sourceObjectBehaviour.gameObject.transform.parent);
			if (result.Successful)
			{
				if (expose)
				{
					//Try start fire if possible
					var reactionManager = MatrixManager.AtPoint(worldPos, true).ReactionManager;
					reactionManager.ExposeHotspotWorldPosition(worldPos.To2Int(), 1000);
				}

				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Sparks, worldPos, sourceObj: sourceObjectBehaviour.gameObject);

				return result.GameObject;
			}

			return null;
		}

		/// <summary>
		/// Try spark at world pos
		/// </summary>
		/// <returns>Null if the spark failed, else the new spark object.</returns>
		public static GameObject TrySpark(Vector3 worldPos, float chanceToSpark = 75, bool expose = true)
		{
			//Clamp just in case
			chanceToSpark = Mathf.Clamp(chanceToSpark, 1, 100);

			//E.g will have 25% chance to not spark when chanceToSpark = 75
			if(DMMath.Prob(100 - chanceToSpark)) return null;

			var result = Spawn.ServerPrefab(CommonPrefabs.Instance.SparkEffect, worldPos);
			if (result.Successful)
			{
				if (expose)
				{
					//Try start fire if possible
					var reactionManager = MatrixManager.AtPoint(worldPos.RoundToInt(), true).ReactionManager;
					reactionManager.ExposeHotspotWorldPosition(worldPos.To2Int(), 1000);
				}

				SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Sparks, worldPos);

				return result.GameObject;
			}

			return null;
		}
	}
}
