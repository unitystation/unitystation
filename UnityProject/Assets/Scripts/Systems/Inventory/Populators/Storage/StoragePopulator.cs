using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Systems.Storage
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

		public override void PopulateItemStorage(ItemStorage ItemStorage, PopulationContext context, SpawnInfo info)
		{
			//Uses the old contents for now
			foreach (var gameObject in DeprecatedContents)
			{
				var ItemSlot = ItemStorage.GetNextFreeIndexedSlot();

				var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);

				if (Validations.CanFit(ItemSlot, spawn.GameObject, NetworkSide.Server) == false)
				{
					Loggy.LogError($"Your initial contents spawn for Storage {ItemStorage.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
				}

				Inventory.ServerAdd(spawn.GameObject, ItemSlot, IgnoreRestraints: true);
			}

			Inventory.PopulateSubInventory(ItemStorage, SlotContents, info);
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

		public void PopulateItemStorage(ItemStorage ItemStorage, PopulationContext context, SpawnInfo info)
		{
			foreach (var gameObject in DeprecatedContents)
			{
				if (gameObject == null) continue;
				var ItemSlot = ItemStorage.GetNextFreeIndexedSlot();
				var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);

				if (Validations.CanFit(ItemSlot, spawn.GameObject, NetworkSide.Server) == false)
				{
					Loggy.LogError($"Your initial contents spawn for ItemStorage {ItemStorage.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
				}


				Inventory.ServerAdd(spawn.GameObject, ItemSlot, IgnoreRestraints: true);
			}

			Inventory.PopulateSubInventory(ItemStorage, SlotContents, info);
		}
	}
}
