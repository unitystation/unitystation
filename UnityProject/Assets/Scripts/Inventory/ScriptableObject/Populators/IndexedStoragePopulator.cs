
using UnityEngine;

/// <summary>
/// Storage populator which populates each slot in an indexed storage.
/// </summary>
[CreateAssetMenu(fileName = "IndexedStoragePopulator", menuName = "Inventory/Populators/IndexedStoragePopulator")]
public class IndexedStoragePopulator : ItemStoragePopulator
{
	[Tooltip("What to put in each indexed slot of this storage.")]
	public SlotContents[] Contents;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		for (var i = 0; i < Contents.Length; i++)
		{
			var slot = toPopulate.GetIndexedItemSlot(i);
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
