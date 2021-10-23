using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
	public class RandomItemSpot : NetworkBehaviour, IServerSpawn
	{
		[Tooltip("Amount of items we could get from this pool")] [SerializeField]
		private int lootCount = 1;
		[Tooltip("Should we spread the items in the tile once spawned?")][SerializeField]
		private bool fanOut = false;
		[Tooltip("List of possible pools of items to choose from")][SerializeField]
		private List<PoolData> poolList = null;

		public GameObject spawnedItem = null;

		private const int MaxAmountRolls = 5;

		public void OnSpawnServer(SpawnInfo info)
		{
			RollRandomPool(true);
		}

		public void RollRandomPool(bool UnrestrictedAndspawn)
		{
			var RegisterTile = this.GetComponent<RegisterTile>();
			for (int i = 0; i < lootCount; i++)
			{
				PoolData pool = null;
				// Roll attempt is a safe check in the case the mapper did tables with low % and we can't find
				// anything to spawn in 5 attempts
				int rollAttempt = 0;

				while (pool == null)
				{

					if (rollAttempt >= MaxAmountRolls)
					{
						break;
					}

					var tryPool = poolList.PickRandom();
					if (DMMath.Prob(tryPool.Probability))
					{
						pool = tryPool;
					}
					else
					{
						rollAttempt ++;
					}
				}

				if (pool == null)
				{
					continue;
				}

				GenerateItem(pool, UnrestrictedAndspawn);
				if (UnrestrictedAndspawn == false) return;
			}

			if (UnrestrictedAndspawn)
			{
				RegisterTile.Matrix.MetaDataLayer.InitialObjects[this.gameObject] = this.transform.localPosition;
				this.GetComponent<CustomNetTransform>().DisappearFromWorldServer(true);
				this.GetComponent<RegisterTile>().UpdatePositionServer();
			}
		}

		private void GenerateItem(PoolData poolData, bool spawn)
		{
			if (poolData == null) return;

            var itemPool = poolData.RandomItemPool;

            if (itemPool == null)
			{
				Logger.LogError($"Item pool was null in {gameObject.name}", Category.ItemSpawn);
				return;
			}

			var item = poolData.RandomItemPool.Pool.PickRandom();
			var spread = fanOut ? Random.Range(-0.5f,0.5f) : (float?) null;

			if (!DMMath.Prob(item.Probability))
			{
				return;
			}

			var maxAmt = Random.Range(1, item.MaxAmount+1);

			this.spawnedItem = item.Prefab;
			if (spawn == false) return;

			var worldPos = gameObject.AssumedWorldPosServer();
			var pushPull = GetComponent<PushPull>();
			Spawn.ServerPrefab(item.Prefab, worldPos, count: maxAmt, scatterRadius: spread, sharePosition: pushPull);
		}
	}

	[Serializable]
	public class PoolData
	{
		[Tooltip("Probabilities of spawning from this pool when chosen")] [SerializeField][Range(0,100)]
		private int probability = 100;
		[Tooltip("The pool from which we will try to get items")] [SerializeField]
		private RandomItemPool randomItemPool = null;
		public int Probability => probability;
		public RandomItemPool RandomItemPool => randomItemPool;
	}
}
