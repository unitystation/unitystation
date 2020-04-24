using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
	public class RandomItemSpot : MonoBehaviour
	{
		[Tooltip("Amout of items we could get from this pool")] [SerializeField]
		private int lootCount = 1;
		[Tooltip("Should we spread the items in the tile once spawned?")][SerializeField]
		private bool fanOut = false;
		[Tooltip("List of possible pools of items to choose from")][SerializeField]
		private List<PoolData> poolList;

		private void Awake()
		{
			EventManager.AddHandler(EVENT.RoundStarted, RollRandomPool);
		}

		private void RollRandomPool()
		{
			foreach (var pool in poolList)
			{
				if (pool == null)
				{
					continue;
				}

				if (!DMMath.Prob(pool.Probability))
				{
					continue;
				}

				SpawnItems(pool);
				break;
			}
		}

		private void SpawnItems(PoolData poolData)
		{


			for (int i = 0; i < lootCount; i++)
			{
				var item = poolData.RandomItemPool.Pool.PickRandom();
				var spread = fanOut ? Random.Range(-0.5f,0.5f) : (float?) null;

				if (!DMMath.Prob(item.Probability))
				{
					continue;
				}

				var maxAmt = Random.Range(1, item.MaxAmount+1);

				Spawn.ServerPrefab(
					item.Prefab,
					gameObject.RegisterTile().WorldPositionServer,
					count: maxAmt,
					scatterRadius: spread);
			}

			Despawn.ServerSingle(gameObject);
		}
	}

	[Serializable]
	public class PoolData
	{
		[Tooltip("Probabilities of spawning from this pool when chosen")] [SerializeField][Range(0,100)]
		private int probability = 100;
		[Tooltip("The pool from which we will try to get items")] [SerializeField]
		private RandomItemPool randomItemPool;
		public int Probability => probability;
		public RandomItemPool RandomItemPool => randomItemPool;
	}
}
