
using UnityEngine;

/// <summary>
/// Storage populator which populates each slot in an indexed storage.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStoragePopulator", menuName = "Inventory/Populators/Storage/IndexedStoragePopulator")]
public class IndexedStoragePopulator : ItemStoragePopulator
{
	[Tooltip("Populator to use for each indexed storage slot (index in list corresponds" +
	         " to slot index that it will populate).")]
	public SlotPopulator[] SlotPopulators;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		for (var i = 0; i < SlotPopulators.Length; i++)
		{
			var slot = toPopulate.GetIndexedItemSlot(i);
			if (slot == null)
			{
				Logger.LogErrorFormat("Item in slot {0} does not have a slot with index {1}. Please ensure" +
				                      " the Contents don't exceed the number of slots in the ItemStorage.", Category.Inventory,
					toPopulate, i);
				return;
			}
			SlotPopulators[i].PopulateSlot(slot, context);
		}
	}
}
