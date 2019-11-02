
using UnityEngine;

/// <summary>
/// Populates each named slot using a particular prefab, cloth, or slot populator.
/// </summary>
public class NamedSlotStoragePopulator : ItemStoragePopulator
{
	[System.Serializable]
	public class NamedSlotPopulatorEntry
	{
		[Tooltip("Named slot being populated. A NamedSlot should not appear" +
		         " more than once in these entries.")]
		public NamedSlot NamedSlot;

		[Tooltip("Determines what will be used from the settings in this entry to populate the slot." +
		         " Prefab will simply spawn a prefab in the slot. Cloth will spawn a cloth with" +
		         " the configured cloth settings. Slot Populator will use the provided slot populator.")]
		public NamedSlotPopulatorEntryType EntryType;


		[Tooltip("Used if Entry type is Slot populator. Slot populator to use to populate this slot.")]
		public SlotPopulator SlotPopulator;

		[Tooltip("Used if Entry type is Prefab or Cloth. Prefab to instantiate and store in this slot. If Entry type is Cloth, " +
		         " leave blank to use the default prefab for that cloth or specify a cloth prefab variant to use that when spawning the cloth.")]
		public GameObject Prefab;

		[Tooltip("Used if Entry type is Cloth. Cloth data indicating the cloth to create and put in this cloth.")]
		public BaseClothData ClothData;
		[Tooltip("Variant of the cloth to spawn. Ignored if no ClothData specified")]
		public ClothingVariantType ClothingVariantType = ClothingVariantType.Default;
		[Tooltip("Index of the variant of the cloth to spawn. ")]
		public int ClothVariantIndex = -1;


	}

	[Tooltip("What to use to populate each named slot")]
	public NamedSlotPopulatorEntry[] Entries;

	public override void PopulateItemStorage(ItemStorage toPopulate)
	{
		Logger.LogTraceFormat("Populating item storage {0}", Category.Inventory, toPopulate.name);
		foreach (var entry in Entries)
		{
			var slot = toPopulate.GetNamedItemSlot(entry.NamedSlot);
			if (slot == null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because it doesn't exist in this itemstorage {1}.",
					Category.Inventory, entry.NamedSlot, toPopulate.name);
				continue;
			}
			if (slot.Item != null)
			{
				Logger.LogTraceFormat("Skipping populating slot {0} because it already has an item.",
					Category.Inventory, slot);
				continue;
			}
			//spawning a cloth or a normal prefab?
			if (entry.ClothData != null)
			{
				var cloth = ClothFactory.CreateCloth(entry.ClothData, CVT: entry.ClothingVariantType,
					variant: entry.ClothVariantIndex,
					PrefabOverride: entry.Prefab);
				Inventory.ServerAdd(cloth.GetComponent<Pickupable>(), slot);
			}
			else
			{
				//prefab
				if (entry.Prefab == null)
				{
					Logger.LogErrorFormat("Cannot populate slot {0} because no prefab or clothdata was specified. Please" +
					                      " specify a prefab or clothdata to populate in this slot.",
						Category.Inventory, toPopulate);
				}
				if (entry.Prefab.GetComponent<Pickupable>() == null)
				{
					Logger.LogErrorFormat("Cannot populate slot {0} because prefab {1} does not have Pickupable.",
						Category.Inventory, toPopulate, entry.Prefab.name);
				}
				var item = PoolManager.PoolNetworkInstantiate(entry.Prefab).GetComponent<Pickupable>();

				Inventory.ServerAdd(item, slot);
			}
			Logger.LogTraceFormat("Populated {0}", Category.Inventory, slot);
		}
	}
}


public enum NamedSlotPopulatorEntryType
{
	/// <summary>
	/// Populate using a provided prefab
	/// </summary>
	Prefab,
	/// <summary>
	/// Populate using a provided cloth + settings
	/// </summary>
	Cloth,
	/// <summary>
	/// Populate using a provided slot populator
	/// </summary>
	SlotPopulator
}
