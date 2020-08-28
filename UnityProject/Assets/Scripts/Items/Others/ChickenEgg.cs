using System.Collections;
using NaughtyAttributes;
using UnityEngine;

namespace Items.Others
{
	public class ChickenEgg : MonoBehaviour, IServerSpawn
	{
		[SerializeField, Tooltip("If true, this egg will be fertilized no matter what")]
		private bool forceFertilized = false;

		[SerializeField, Tooltip("How likely is this chicken egg to spawn a little chick"),
		 ShowIf(nameof(forceFertilized))]
		private int fertilizedChance = 0;

		[SerializeField, Tooltip("How long until it hatches (or tries to)"), MinMaxSlider(0f, 999f)]
		private Vector2 hatchingTime = Vector2.zero;

		[SerializeField, Tooltip("Reference to the little chick object")]
		private GameObject chick = null;

		public void OnSpawnServer(SpawnInfo info)
		{
			StartCoroutine(HatchChick());
		}

		public void SetFertilizedChance(int percentage)
		{
			fertilizedChance = percentage;
		}

		private IEnumerator HatchChick()
		{
			yield return WaitFor.Seconds(Random.Range(hatchingTime.x, hatchingTime.y));
			// yield return WaitFor.EndOfFrame;
			if (!forceFertilized && !DMMath.Prob(fertilizedChance))
			{
				yield break;
			}

			Spawn.ServerPrefab(chick, gameObject.RegisterTile().WorldPosition);
			Despawn.ServerSingle(gameObject);
		}
	}
}
