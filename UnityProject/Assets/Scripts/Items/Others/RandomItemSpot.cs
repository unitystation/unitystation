using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using Objects;
using Objects.Engineering;
using Shared.Systems.ObjectConnection;
using Systems.Electricity;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
	public class RandomItemSpot : NetworkBehaviour, IServerSpawn
	{
		public APCPoweredDevice APCtoSet;

		[Tooltip("Amount of items we could get from this pool")] [SerializeField]
		private int lootCount = 1;
		[Tooltip("Should we spread the items in the tile once spawned?")][SerializeField]
		private bool fanOut = false;
		[Tooltip("List of possible pools of items to choose from")][SerializeField]
		private List<PoolData> poolList = null;

		public GameObject spawnedItem = null;

		private const int MaxAmountRolls = 5;

		[SerializeField]
		private bool triggerManually = false;

		public void OnSpawnServer(SpawnInfo info)
		{

			APCtoSet = this.GetComponent<APCPoweredDevice>();
			RollRandomPool(true);

		}

		public void RollRandomPool(bool UnrestrictedAndspawn, bool overrideTrigger = false)
		{
			if(overrideTrigger == false && triggerManually) return;

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
				if (this.GetComponent<RuntimeSpawned>() != null)
				{
					_ = Despawn.ServerSingle(gameObject);
					return;
				}

				if (RegisterTile.TryGetComponent<UniversalObjectPhysics>(out var ObjectBehaviour))
				{
					if (ObjectBehaviour.ContainedInObjectContainer != null)
					{
						//TODO Do item storage
						if (ObjectBehaviour.ContainedInObjectContainer.TryGetComponent<ObjectContainer>(out var ObjectContainer))
						{
							ObjectContainer.RetrieveObject(this.gameObject);
						}
					}
				}


				APCtoSet.OrNull()?.RemoveFromAPC();
				RegisterTile.Matrix.MetaDataLayer.InitialObjects[this.gameObject] = this.transform.localPosition;
				this.GetComponent<UniversalObjectPhysics>()?.DisappearFromWorld();
				this.GetComponent<RegisterTile>().UpdatePositionServer();
			}
		}

		private void GenerateItem(PoolData poolData, bool spawn)
		{
			if (poolData == null) return;

            var itemPool = poolData.RandomItemPool;

            if (itemPool == null)
			{
				Loggy.LogError($"Item pool was null in {gameObject.name}", Category.ItemSpawn);
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
			var pushPull = GetComponent<UniversalObjectPhysics>();
			var Newobjt = Spawn.ServerPrefab(item.Prefab, worldPos, count: maxAmt, scatterRadius: spread, sharePosition: pushPull).GameObject;
			if (APCtoSet != null && APCtoSet.RelatedAPC != null)
			{
				var newAPCtoSet = Newobjt.GetComponent<APCPoweredDevice>();
				if (newAPCtoSet != null)
				{
					((IMultitoolSlaveable) newAPCtoSet).TrySetMaster(null, APCtoSet.RelatedAPC);
				}
			}
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
