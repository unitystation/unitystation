
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// ItemStorageCapacity which should cover most typical use cases, allowing you to define all the aspects of
/// what is allowed.
/// </summary>
public class DefinedCapacity : ItemStorageCapacity
{
	[System.Serializable]
	public class DefinedCapacityEntry
	{
		[Tooltip("Max item size allowed.")]
		public ItemSize MaxItemSize = ItemSize.Huge;

		[Tooltip("Item types allowed.")]
		public ItemType[] AllowedItemTypes = { ItemType.All };

		public bool CanFit(Pickupable toCheck)
		{
			if (toCheck == null) return false;
			ItemSize size = ItemSize.Huge;
			var itemAttrs = toCheck.GetComponent<ItemAttributes>();
			if (itemAttrs != null)
			{
				size = itemAttrs.size;
			}
			else
			{
				Logger.LogTraceFormat("{0} has no item attrs, defaulting to ItemSize.Huge", Category.Inventory, toCheck.name);
			}

			if (size > MaxItemSize)
			{
				Logger.LogTraceFormat("{0} ({1}) exceeds max size of slot ({2})", Category.Inventory, toCheck.name, size, MaxItemSize);
				return false;
			}

			if (AllowedItemTypes.Length == 0)
			{
				Logger.LogTrace("No item types are allowed in this slot. This may be a config error.", Category.Inventory);
				return false;
			}
			if (AllowedItemTypes.Contains(ItemType.All)) return true;
			ItemType type = ItemType.None;
			if (itemAttrs != null)
			{
				type = itemAttrs.itemType;
			}
			else
			{
				Logger.LogTraceFormat("{0} has no item attrs, defaulting to ItemType.None", Category.Inventory, toCheck.name);
			}

			if (AllowedItemTypes.Contains(type))
			{
				return true;
			}
			else
			{
				Logger.LogTraceFormat("Item {0} with type {1} does not match allowed item types {2}.", Category.Inventory,
					toCheck.name, type, String.Join(",", AllowedItemTypes.Select(ait => ait.ToString())));
				return false;
			}

		}
	}

	[System.Serializable]
	public class NamedDefinedCapacityEntry
	{
		[Tooltip("Slot this is for")]
		public NamedSlot NamedSlot;
		[Tooltip("Capacity of the indicated named slot.")]
		public DefinedCapacityEntry Capacity;
	}

	[Tooltip("Capacity capabilities of all indexed slots")]
	public DefinedCapacityEntry IndexedSlotCapacity;

	[Tooltip("Capacity capabilities of each named slot")]
	public NamedDefinedCapacityEntry[] NamedSlotCapacity;

	public override bool CanFit(Pickupable toCheck, ItemSlot inSlot)
	{
		Logger.LogTraceFormat("Checking if {0} can fit in {1}", Category.Inventory, toCheck.name, inSlot);
		//which type of slot are we checking
		if (inSlot.SlotIdentifier.SlotIdentifierType == SlotIdentifierType.Indexed)
		{
			if (IndexedSlotCapacity == null)
			{
				Logger.LogTrace("Indexed slot capacity not defined. Assuming no fit.", Category.Inventory);
				return false;
			}
			return IndexedSlotCapacity.CanFit(toCheck);
		}
		else
		{
			NamedDefinedCapacityEntry entry = NamedSlotCapacity.FirstOrDefault(nsc => nsc.NamedSlot == inSlot.SlotIdentifier.NamedSlot);
			if (entry == null)
			{
				Logger.LogTraceFormat("Slot capacity not defined for {0}. Assuming no fit.", Category.Inventory, toCheck.name);
				return false;
			}

			return entry.Capacity.CanFit(toCheck);
		}
	}
}
