using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Lifecycle;

namespace US13.Systems.Inventory.Populators.Storage
{
	/// <summary>
	/// Storage populator which populates each slot in an indexed storage.
	/// </summary>
	[CreateAssetMenu(fileName = "IndexedStoragePopulator",
		menuName = "Inventory/Populators/Storage/IndexedStoragePopulator")]
	public class StoragePopulator : ItemStoragePopulator
	{
		[SerializeField]
		[Tooltip("Prefabs to spawn in each storage slot")]
		private List<SlotPopulatorEntry> SlotContents = new List<SlotPopulatorEntry>();

		[FormerlySerializedAs("Contents")]
		[SerializeField]
		[Tooltip("**Deprecated**Prefabs to spawn in each indexed storage slot (index in list corresponds" +
				 " to slot index that it will populate). **Deprecated**")]
		private List<GameObject> DeprecatedContents = new List<GameObject>();

		public override void PopulateItemStorage(IStoreThings ItemStorage, MonoBehaviour component, PopulationContext context, SpawnInfo info)
		{
			//Uses the old contents for now
			foreach (var gameObject in DeprecatedContents)
			{
				var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);


				if (ItemStorage.CanFit(spawn.GameObject)== false)
				{
					Loggy.Error($"Your initial contents spawn for Storage {component.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
				}

				ItemStorage.ServerTryAdd(spawn.GameObject, IgnoreRestraints: true);

			}

			if (component is ItemStorage storage)
			{
				Inventory.PopulateSubInventory(storage, SlotContents, info);
			}
		}
	}

	[Serializable]
	public class PrefabListPopulater : IItemStoragePopulator
	{
		[SerializeField]
		[Tooltip("Prefabs to spawn in each storage slot")]
		public List<SlotPopulatorEntry> SlotContents = new List<SlotPopulatorEntry>();

		[FormerlySerializedAs("Contents")]
		[SerializeField]
		[Tooltip("**Deprecated**Prefabs to spawn in each indexed storage slot (index in list corresponds" +
				 " to slot index that it will populate). **Deprecated**")]
		public List<GameObject> DeprecatedContents = new List<GameObject>();


		public List<GameObject> GetFirstLayerDeprecatedAndNew()
		{
			var Returning = new List<GameObject>();
			Returning.AddRange(DeprecatedContents);
			foreach (var SlotContent in SlotContents)
			{
				Returning.Add(SlotContent.Prefab);
			}

			return Returning;
		}

		public void PopulateItemStorage(IStoreThings ItemStorage, MonoBehaviour component, PopulationContext context, SpawnInfo info)
		{
			foreach (var gameObject in DeprecatedContents)
			{
				if (gameObject == null) continue;
				var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);

				if (ItemStorage.CanFit(spawn.GameObject)== false)
				{
					Loggy.Trace($"Your initial contents spawn for ItemStorage {component.name} for {spawn.GameObject} Is bypassing the Can fit requirements", Category.Inventory);
				}

				ItemStorage.ServerTryAdd(spawn.GameObject, IgnoreRestraints: true);

			}

			if (component is ItemStorage storage)
			{
				Inventory.PopulateSubInventory(storage, SlotContents, info);
			}


		}
	}
}
