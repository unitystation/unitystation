
using UnityEngine;

/// <summary>
/// Populates a slot with a specified cloth.
/// </summary>
[CreateAssetMenu(fileName = "ClothSlotPopulator", menuName = "Inventory/Populators/Slot/ClothSlotPopulator")]
public class ClothSlotPopulator : SlotPopulator
{
	[SerializeField]
	[Tooltip("Cloth data indicating the cloth to create and put in this cloth.")]
	private BaseClothData ClothData;

	[SerializeField]
	[Tooltip("Variant of the cloth to spawn.")]
	private ClothingVariantType ClothingVariantType = ClothingVariantType.Default;

	[SerializeField]
	[Tooltip("Index of the variant of the cloth to spawn. ")]
	private int ClothVariantIndex = -1;

	[SerializeField]
	[Tooltip("Prefab override to use. Leave blank to use the default prefab for this cloth.")]
	private GameObject PrefabOverride;


	public override void PopulateSlot(ItemSlot slot, PopulationContext context)
	{
		if (slot.Item != null)
		{
			Logger.LogTraceFormat("Skipping populating slot {0} because it already has an item.",
				Category.Inventory, slot);
			return;
		}
		Logger.LogTraceFormat("Populating {0} using cloth data {1}, variant type {2}, variant index {3}", Category.Inventory, slot,
			ClothData, ClothingVariantType, ClothVariantIndex);
		if (ClothData == null)
		{
			Logger.LogErrorFormat("Cannot populate slot {0} because no clothdata was specified. Please" +
			                      " specify a prefab or clothdata to populate in this slot.",
				Category.Inventory, slot);
			return;
		}
		var cloth = Spawn.ServerCloth(ClothData, CVT: ClothingVariantType,
			variantIndex: ClothVariantIndex,
			prefabOverride: PrefabOverride).GameObject;
		Inventory.ServerAdd(cloth, slot);

		Logger.LogTraceFormat("Populated {0}", Category.Inventory, slot);
	}
}
