
using System;
using UnityEngine;

/// <summary>
/// Defines the contents that should go in a particular slot.
/// </summary>
[Serializable]
public class SlotContents
{
	[Tooltip("Determines what will be used from this to populate the slot." +
	         " Prefab will simply spawn a prefab in the slot. Cloth will spawn a cloth with" +
	         " the configured cloth settings. Slot Populator will use the provided slot populator.")]
	public SlotContentsType ContentsType;

	[Tooltip("Used if Entry type is Cloth. Cloth data indicating the cloth to create and put in this cloth.")]
	public BaseClothData ClothData;
	[Tooltip("Variant of the cloth to spawn. Ignored if no ClothData specified")]
	public ClothingVariantType ClothingVariantType = ClothingVariantType.Default;
	[Tooltip("Index of the variant of the cloth to spawn. ")]
	public int ClothVariantIndex = -1;


	[Tooltip("Used if Entry type is Prefab or Cloth. Prefab to instantiate and store in this slot. If Entry type is Cloth, " +
	         " leave blank to use the default prefab for that cloth or specify a cloth prefab variant to use that when spawning the cloth.")]
	public GameObject Prefab;

	[Tooltip("Used if Entry type is Slot populator. Slot populator to use to populate this slot. This can be useful" +
	         " if you need to perform special logic in order to spawn / configure what goes into this slot, you can" +
	         " create your own SlotPopulator SO and put it in here.")]
	public SlotPopulator SlotPopulator;

	/// <summary>
	/// Populates the specified slot using the specified config.
	/// </summary>
	/// <param name="toPopulate"></param>
	public void PopulateItemSlot(ItemSlot slot)
	{
		if (slot.Item != null)
		{
			Logger.LogTraceFormat("Skipping populating slot {0} because it already has an item.",
				Category.Inventory, slot);
			return;
		}

		if (ContentsType == SlotContentsType.Prefab)
		{
			Logger.LogTraceFormat("Populating {0} using prefab {1}", Category.Inventory, slot, Prefab);
			if (Prefab == null)
			{
				Logger.LogErrorFormat("Cannot populate slot {0} because no prefab was specified. Please" +
				                      " specify a prefab or clothdata to populate in this slot.",
					Category.Inventory, slot);
				return;
			}
			if (Prefab.GetComponent<Pickupable>() == null)
			{
				Logger.LogErrorFormat("Cannot populate slot {0} because prefab {1} does not have Pickupable.",
					Category.Inventory, slot, Prefab.name);
				return;
			}
			var item = PoolManager.PoolNetworkInstantiate(Prefab).GetComponent<Pickupable>();

			Inventory.ServerAdd(item, slot);
		}
		else if (ContentsType == SlotContentsType.Cloth)
		{
			Logger.LogTraceFormat("Populating {0} using cloth data {1}, variant type {2}, variant index {3}", Category.Inventory, slot,
				ClothData, ClothingVariantType, ClothVariantIndex);
			if (ClothData == null)
			{
				Logger.LogErrorFormat("Cannot populate slot {0} because no clothdata was specified. Please" +
				                      " specify a prefab or clothdata to populate in this slot.",
					Category.Inventory, slot);
				return;
			}
			var cloth = ClothFactory.CreateCloth(ClothData, CVT: ClothingVariantType,
				variant: ClothVariantIndex,
				PrefabOverride: Prefab);
			Inventory.ServerAdd(cloth, slot);
		}
		else if (ContentsType == SlotContentsType.SlotPopulator)
		{
			SlotPopulator.PopulateSlot(slot);
		}

		Logger.LogTraceFormat("Populated {0}", Category.Inventory, slot);
	}
}


public enum SlotContentsType
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
