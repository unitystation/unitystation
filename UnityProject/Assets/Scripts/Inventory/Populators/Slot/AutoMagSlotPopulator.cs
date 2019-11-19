
using UnityEngine;

/// <summary>
/// Only works when populating a slot on a Gun. Automatically populates the slot with
/// the proper magazine.
/// </summary>
[CreateAssetMenu(fileName = "AutoMagSlotPopulator", menuName = "Inventory/Populators/Slot/AutoMagSlotPopulator")]
public class AutoMagSlotPopulator : SlotPopulator
{
	public override void PopulateSlot(ItemSlot slot, PopulationContext context)
	{
		Logger.LogTraceFormat("Trying to auto-populate magazine for {0}", Category.Inventory, slot);

		var gun = slot.ItemStorage.GetComponent<Gun>();
		if (gun == null)
		{
			Logger.LogErrorFormat("Cannot auto-populate magazine, itemstorage containing this slot does not have Gun component", Category.Inventory);
			return;
		}
		var ammoPrefab = Resources.Load("Rifles/Magazine_" + gun.AmmoType) as GameObject;
		Logger.LogTraceFormat("Populating with ammo prefab {0}", Category.Inventory, ammoPrefab?.name);
		GameObject m = Spawn.ServerPrefab(ammoPrefab).GameObject;
		Inventory.ServerAdd(m, slot);
	}
}
