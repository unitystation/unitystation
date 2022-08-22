using ScriptableObjects;
using UnityEngine;

namespace Systems.Explosions
{
	public static class SparkUtil
	{
		/// <summary>
		/// Try spark using an gameObject
		/// (USE ONLY FOR SINGLE ONE OF USE, NOT LOOPING)
		/// (See lightsource for alternative)
		/// </summary>
		public static void TrySpark(GameObject originator, float chanceToSpark = 100, bool expose = true)
		{
			InternalSpark(originator, originator.AssumedWorldPosServer(), chanceToSpark, expose);
		}

		/// <summary>
		/// Try spark at world pos
		/// </summary>
		public static void TrySpark(Vector3 worldPos, float chanceToSpark = 100, bool expose = true)
		{
			InternalSpark(null, worldPos, chanceToSpark, expose);
		}

		private static void InternalSpark(GameObject gameObject, Vector3 worldPos, float chanceToSpark, bool expose)
		{
			//Clamp just in case
			chanceToSpark = Mathf.Clamp(chanceToSpark, 1, 100);

			//E.g will have 25% chance to not spark when chanceToSpark = 75
			if(DMMath.Prob(100 - chanceToSpark)) return;

			var result = Spawn.ServerPrefab(CommonPrefabs.Instance.SparkEffect,
				worldPos,
				gameObject != null ? gameObject.transform.parent : null);

			if (result.Successful == false) return;

			if (expose)
			{
				//Try start fire if possible
				var reactionManager = MatrixManager.AtPoint(worldPos.RoundToInt(), true).ReactionManager;
				reactionManager.ExposeHotspotWorldPosition(worldPos.RoundTo2Int(), 1000);
			}

			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.Sparks, worldPos,
				sourceObj: gameObject.OrNull());
		}
	}
}
