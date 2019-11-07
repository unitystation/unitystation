
using UnityEngine;

/// <summary>
/// Populates a slot with a specified prefab.
/// </summary>
[CreateAssetMenu(fileName = "PrefabSlotPopulator", menuName = "Inventory/Populators/Slot/PrefabSlotPopulator")]
public class PrefabSlotPopulator : SlotPopulator
{
	[SerializeField]
	[Tooltip("Prefab to instantiate and populate in the slot. Must have Pickupable.")]
	private GameObject Prefab;

	/// <summary>
	/// Populates the specified slot using the specified config.
	/// </summary>
	/// <param name="toPopulate"></param>
	public override void PopulateSlot(ItemSlot slot, PopulationContext context)
	{
		if (slot.Item != null)
		{
			Logger.LogTraceFormat("Skipping populating slot {0} because it already has an item.",
				Category.Inventory, slot);
			return;
		}

		Logger.LogTraceFormat("Populating {0} using prefab {1}", Category.Inventory, slot, Prefab);
		if (Prefab == null)
		{
			Logger.LogErrorFormat("Cannot populate slot {0} because no prefab was specified. Please" +
			                      " specify a prefab to populate in this slot.",
				Category.Inventory, slot);
			return;
		}
		if (Prefab.GetComponent<Pickupable>() == null)
		{
			Logger.LogErrorFormat("Cannot populate slot {0} because prefab {1} does not have Pickupable.",
				Category.Inventory, slot, Prefab.name);
			return;
		}
		var item = Spawn.ServerPrefab(Prefab).GameObject.GetComponent<Pickupable>();

		Inventory.ServerAdd(item, slot);

		Logger.LogTraceFormat("Populated {0}", Category.Inventory, slot);
	}
}
