using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Storage populator which populates each slot in an indexed storage.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStoragePopulator",
	menuName = "Inventory/Populators/Storage/IndexedStoragePopulator")]
public class IndexedStoragePopulator : ItemStoragePopulator
{
	/// <summary>
	/// Defines what to do when the slot already contains an item
	/// </summary>
	public enum IndexedMergeMode
	{
		/// <summary>
		/// Leaves the item that's already there and tries to spawn our item in the next available slot.
		/// </summary>
		Append = 0,

		/// <summary>
		/// Despawns the item in the slot, replacing it with our item
		/// </summary>
		Overwrite = 1
	}

	[SerializeField]
	[Tooltip("Prefabs to spawn in each indexed storage slot (index in list corresponds" +
	         " to slot index that it will populate).")]
	private GameObject[] Contents = null;
	public GameObject[] Content => Contents;


	[SerializeField] [Tooltip("What to do if the storage already has an item in a particular slot.")]
	private IndexedMergeMode MergeMode = IndexedMergeMode.Append;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		//if appending, start at first free slot index
		var start = 0;
		if (MergeMode == IndexedMergeMode.Append)
		{
			var freeSlot = toPopulate.GetNextFreeIndexedSlot();
			if (freeSlot == null)
			{
				Logger.LogTraceFormat("Can't populate {0}, no more free slots.", Category.EntitySpawn, toPopulate);
				return;
			}

			start = freeSlot.SlotIdentifier.SlotIndex;
		}

		for (var i = start; i < Contents.Length; i++)
		{
			var slot = toPopulate.GetIndexedItemSlot(i);
			if (slot == null)
			{
				Logger.LogErrorFormat("Storage {0} does not have a slot with index {1}. Please ensure" +
				                      " the Contents don't exceed the number of slots in the ItemStorage.",
									  Category.EntitySpawn, toPopulate, i);
				return;
			}

			if (slot.Item != null && MergeMode == IndexedMergeMode.Overwrite)
			{
				Inventory.ServerDespawn(slot);
			}

			// General protection against missing items
			if (Contents[i] == null)
			{
				Logger.LogError($"Item is missing at position {i} of {toPopulate.name}", Category.EntitySpawn);
				continue; // Will skip the missing item
			}

			var spawned = Spawn.ServerPrefab(Contents[i]).GameObject;
			Inventory.ServerAdd(spawned, slot);
		}
	}
}


[System.Serializable]
public class PrefabListPopulater : IItemStoragePopulator
{
	[SerializeField] [Tooltip("What to do if the storage already has an item in a particular slot.")]
	private IndexedStoragePopulator.IndexedMergeMode MergeMode = IndexedStoragePopulator.IndexedMergeMode.Append;

	[SerializeField]
	[Tooltip("Prefabs to spawn in each indexed storage slot (index in list corresponds" +
	         " to slot index that it will populate).")]
	public List<GameObject> Contents = new List<GameObject>();

	public void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		//if appending, start at first free slot index
		var start = 0;
		if (MergeMode == IndexedStoragePopulator.IndexedMergeMode.Append)
		{
			var freeSlot = toPopulate.GetNextFreeIndexedSlot();
			if (freeSlot == null)
			{
				Logger.LogTraceFormat("Can't populate {0}, no more free slots.", Category.Inventory, toPopulate);
				return;
			}

			start = freeSlot.SlotIdentifier.SlotIndex;
		}

		for (var i = start; i < Contents.Count; i++)
		{
			// General protection against missing items
			if (Contents[i] == null)
			{
				continue; // Will skip the missing item
			}

			var slot = toPopulate.GetIndexedItemSlot(i);
			if (slot == null)
			{
				Logger.LogErrorFormat("Storage {0} does not have a slot with index {1}. Please ensure" +
				                      " the Contents don't exceed the number of slots in the ItemStorage.",
					Category.Inventory,
					toPopulate, i);
				return;
			}

			if (slot.Item != null && MergeMode == IndexedStoragePopulator.IndexedMergeMode.Overwrite)
			{
				Inventory.ServerDespawn(slot);
			}

			var spawned = Spawn.ServerPrefab(Contents[i]).GameObject;
			Inventory.ServerAdd(spawned, slot);
		}
	}
}