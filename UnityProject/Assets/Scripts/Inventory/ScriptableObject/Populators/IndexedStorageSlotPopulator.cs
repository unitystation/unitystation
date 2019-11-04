
using UnityEngine;

/// <summary>
/// Slot populator which populates a slot with something that has an indexed ItemStorage,
/// and then populates each of the indexed slots in the ItemStorage with the specified contents.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStorageSlotPopulator", menuName = "Inventory/Populators/IndexedStorageSlotPopulator")]
public class IndexedStorageSlotPopulator : SlotPopulator
{
	[Tooltip("What to populate in this slot. The populated item must have" +
	         " ItemStorage.")]
	public SlotContents Item;

	[Tooltip("What to put in each indexed slot of the item.")]
	public SlotContents[] Contents;

	public override void PopulateSlot(ItemSlot toPopulate, PopulationContext context)
	{
		Item.PopulateItemSlot(toPopulate, context);

		var storage = toPopulate.Item.GetComponent<ItemStorage>();
		if (storage == null)
		{
			Logger.LogErrorFormat("Item in slot {0} does not have an ItemStorage to populate, please ensure Item" +
			                      " creates an object with ItemStorage", Category.Inventory,
				toPopulate);
			return;
		}

		for (var i = 0; i < Contents.Length; i++)
		{
			var slot = storage.GetIndexedItemSlot(i);
			if (slot == null)
			{
				Logger.LogErrorFormat("Item in slot {0} does not have a slot with index {1}. Please ensure" +
				                      " the Contents don't exceed the number of slots in the ItemStorage.", Category.Inventory,
					toPopulate, i);
				return;
			}
			Contents[i].PopulateItemSlot(slot, context);
		}
	}
}
