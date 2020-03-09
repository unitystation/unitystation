
using UnityEngine;

/// <summary>
/// Storage populator which populates each slot in an indexed storage.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStoragePopulator", menuName = "Inventory/Populators/Storage/IndexedStoragePopulator")]
public class IndexedStoragePopulator : ItemStoragePopulator
{
	/// <summary>
	/// Defines what to do when the slot already contains an item
	/// </summary>
	enum IndexedMergeMode
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


	[SerializeField]
	[Tooltip("What to do if the storage already has an item in a particular slot.")]
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
				Logger.LogTraceFormat("Can't populate {0}, no more free slots.", Category.Inventory, toPopulate);
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
				                      " the Contents don't exceed the number of slots in the ItemStorage.", Category.Inventory,
					toPopulate, i);
				return;
			}

			if (slot.Item != null && MergeMode == IndexedMergeMode.Overwrite)
			{
				Inventory.ServerDespawn(slot);
			}

			// General protection against missing items
			if (Contents[i] == null)
			{
				Logger.LogError($"Item is missing at position {i} of {toPopulate.name}");
				continue; // Will skip the missing item
			}

			var spawned = Spawn.ServerPrefab(Contents[i]).GameObject;
			Inventory.ServerAdd(spawned, slot);
		}
	}
}


