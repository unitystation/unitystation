
using UnityEngine;

/// <summary>
/// Slot populator which populates a slot with something that has an indexed ItemStorage,
/// and then populates each of the indexed slots in the ItemStorage with the specified Indexed Storage Populator.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStorageSlotPopulator", menuName = "Inventory/Populators/Slot/IndexedStorageSlotPopulator")]
public class IndexedStorageSlotPopulator : SlotPopulator
{
	[SerializeField]
	[Tooltip("Populator to use to populate this slot. The populated item must have itemstorage.")]
	private SlotPopulator SlotPopulator;

	[SerializeField]
	[Tooltip("Indexed storage populator to populate each slot in the storage.")]
	private IndexedStoragePopulator IndexedStoragePopulator;

	public override void PopulateSlot(ItemSlot slot, PopulationContext context)
	{
		SlotPopulator.PopulateSlot(slot, context);

		var storage = slot.Item.GetComponent<ItemStorage>();
		if (storage == null)
		{
			Logger.LogErrorFormat("Item in slot {0} does not have an ItemStorage to populate, please ensure Item" +
			                      " creates an object with ItemStorage", Category.Inventory,
				slot);
			return;
		}

		if (IndexedStoragePopulator == null)
		{
			Logger.LogTraceFormat("Not populating indexed storage in {0} because the indexed storage slot " +
			                      "populator is unspecified.", Category.Inventory, slot);
			return;
		}

		IndexedStoragePopulator.PopulateItemStorage(storage, context);
	}
}
