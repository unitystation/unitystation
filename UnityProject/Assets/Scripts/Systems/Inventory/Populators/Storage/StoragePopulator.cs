using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

	public override void PopulateItemStorage(ItemStorage ItemStorage, PopulationContext context)
	{
		//Uses the old contents for now
		foreach (var gameObject in DeprecatedContents)
		{
			var ItemSlot = ItemStorage.GetNextFreeIndexedSlot();
			var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true);
			Inventory.ServerAdd(spawn.GameObject, ItemSlot, IgnoreRestraints:  true );
		}

		if (SlotContents.Count == 0)  return;

		//Look through the specified slots, and tries to find it on the storage and populates if so,
		//and then attempts recursion
		foreach (var namedSlotPopulatorEntry in SlotContents)
		{
			ItemSlot ItemSlot;
			if (namedSlotPopulatorEntry.UesIndex)
			{
				ItemSlot = ItemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
			}
			else
			{
				ItemSlot = ItemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
			}
			if (ItemSlot == null) continue;

			var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true);
			Inventory.ServerAdd(spawn.GameObject, ItemSlot,namedSlotPopulatorEntry.ReplacementStrategy, true );
			Inventory.PopulateSubInventory(spawn.GameObject, namedSlotPopulatorEntry.namedSlotPopulatorEntrys);
		}
	}
}


[System.Serializable]
public class PrefabListPopulater : IItemStoragePopulator
{
	[SerializeField]
	[Tooltip("Prefabs to spawn in each storage slot")]
	private List<SlotPopulatorEntry> SlotContents = new List<SlotPopulatorEntry>();

	[FormerlySerializedAs("Contents")]
	[SerializeField]
	[Tooltip("**Deprecated**Prefabs to spawn in each indexed storage slot (index in list corresponds" +
	         " to slot index that it will populate). **Deprecated**")]
	public List<GameObject> DeprecatedContents = new List<GameObject>();


	public void PopulateItemStorage(ItemStorage ItemStorage, PopulationContext context)
	{
		foreach (var gameObject in DeprecatedContents)
		{
			var ItemSlot = ItemStorage.GetNextFreeIndexedSlot();
			var spawn = Spawn.ServerPrefab(gameObject, PrePickRandom: true);
			Inventory.ServerAdd(spawn.GameObject, ItemSlot, IgnoreRestraints:  true );
		}

		if (SlotContents.Count == 0)  return;

		foreach (var namedSlotPopulatorEntry in SlotContents)
		{
			ItemSlot ItemSlot;
			if (namedSlotPopulatorEntry.UesIndex)
			{
				ItemSlot = ItemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
			}
			else
			{
				ItemSlot = ItemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
			}
			if (ItemSlot == null) continue;

			var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true);
			Inventory.ServerAdd(spawn.GameObject, ItemSlot,namedSlotPopulatorEntry.ReplacementStrategy, true );
			Inventory.PopulateSubInventory(spawn.GameObject, namedSlotPopulatorEntry.namedSlotPopulatorEntrys);
		}
	}
}