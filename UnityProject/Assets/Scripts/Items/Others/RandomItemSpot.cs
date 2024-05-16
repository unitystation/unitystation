using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Mirror;
using NaughtyAttributes;
using Shared.Systems.ObjectConnection;
using Systems;
using Systems.Electricity;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
	public class RandomItemSpot : NetworkBehaviour, IServerSpawn, ICheckedInteractable<HandActivate>
	{
		public APCPoweredDevice APCtoSet;

		[Tooltip("Amount of items we could get from this pool"), MinValue(1)] [SerializeField]
		private int lootCount = 1;

		[Tooltip("Should we spread the items in the tile once spawned?")] [SerializeField]
		private bool fanOut = false;

		[Tooltip("List of possible pools of items to choose from")] [SerializeField]
		private List<PoolData> poolList = null;

		[HideInInspector]
		public GameObject spawnedItem = null;

		private const int MaxAmountRolls = 5;

		[SerializeField] private bool triggerManually = false;
		[SerializeField] private bool automaticallyShoveIntoContainerBelowSpawnedItem = true;
		[SerializeField] private bool deleteAfterUse = true;

		public void OnSpawnServer(SpawnInfo info)
		{
			APCtoSet = this.GetComponent<APCPoweredDevice>();
			RollRandomPool(true);
		}

		public void RollRandomPool(bool UnrestrictedAndspawn, bool overrideTrigger = false)
		{
			if (overrideTrigger == false && triggerManually) return;
			SpawnRandomItems(UnrestrictedAndspawn);
			APCtoSet.OrNull()?.RemoveFromAPC();

		}

		[Button]
		public void SpawnRandomItems(bool unrestrictedAndSpawn = true)
		{
			var spawnAmount = 0;
			for (int i = 0; i < lootCount; i++)
			{
				PoolData pool = GrabRandomPool();
				if (pool == null) continue;
				GenerateItem(pool, unrestrictedAndSpawn);
				spawnAmount++;
				if (unrestrictedAndSpawn == false) break; // what the fuck does UnrestrictedAndspawn mean? What's the point? terrible name
			}
			if (spawnAmount == 0)
			{
				Loggy.LogError($"[RandomItemSpot/SpawnRandomItems] - " +
				               $"No items spawned, expected {lootCount} on {gameObject.name} items but received 0", Category.ItemSpawn);
			}
		}

		private PoolData GrabRandomPool()
		{
			// Roll attempt is a safe check in the case the mapper did tables with low % and we can't find
			// anything to spawn in 5 attempts
			PoolData pool = null;
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
					rollAttempt++;
				}
			}
			return pool;
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
			var spread = fanOut ? Random.Range(-0.5f, 0.5f) : (float?) null;

			if (!DMMath.Prob(item.Probability))
			{
				return;
			}

			var maxAmt = Random.Range(1, item.MaxAmount + 1);

			this.spawnedItem = item.Prefab;
			if (spawn == false) return;

			var worldPos = gameObject.AssumedWorldPosServer();
			var pushPull = GetComponent<UniversalObjectPhysics>();
			var Newobjt = Spawn.ServerPrefab(item.Prefab, worldPos, count: maxAmt, scatterRadius: spread,
				sharePosition: pushPull).GameObject;
			ShoveIntoStorage(Newobjt);
			if (APCtoSet != null && APCtoSet.RelatedAPC != null)
			{
				var newAPCtoSet = Newobjt.GetComponent<APCPoweredDevice>();
				if (newAPCtoSet != null)
				{
					((IMultitoolSlaveable) newAPCtoSet).TrySetMaster(null, APCtoSet.RelatedAPC);
				}
			}
		}

		private void ShoveIntoStorage(GameObject obj)
		{
			if (automaticallyShoveIntoContainerBelowSpawnedItem == false) return;
			if (obj.GetUniversalObjectPhysics().ContainedInObjectContainer != null) return;
			var storages = MatrixManager.GetAt<IUniversalInventoryAPI>(gameObject.AssumedWorldPosServer().CutToInt(), true);
			if (storages == null) return;
			var storageList = storages.ToList();
			if (storageList.Count == 0 || storageList[0] is null) return;
			storageList[0].GrabObjects(new List<GameObject>{obj});
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			var progress = StandardProgressAction.Create(
				new StandardProgressActionConfig(StandardProgressActionType.Construction),
				() => RollRandomPool(true, true));
			progress.ServerStartProgress(interaction.PerformerPlayerScript.ObjectPhysics.registerTile, 1.25f,
				interaction.Performer);
		}
	}

	[Serializable]
	public class PoolData
	{
		[Tooltip("Probabilities of spawning from this pool when chosen")] [SerializeField] [Range(0, 100)]
		private int probability = 100;

		[Tooltip("The pool from which we will try to get items")] [SerializeField]
		private RandomItemPool randomItemPool = null;

		public int Probability => probability;
		public RandomItemPool RandomItemPool => randomItemPool;
	}
}