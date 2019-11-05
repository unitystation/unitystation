
using System;
using System.Linq;
using UnityEngine;

/// <summary>
/// ItemStorageCapacity which should cover most typical use cases, allowing you to define all the aspects of
/// what is allowed.
/// </summary>
[CreateAssetMenu(fileName = "DefinedCapacity", menuName = "Inventory/DefinedCapacity", order = 4)]
public class DefinedCapacity : ItemStorageCapacity
{
	[System.Serializable]
	public class DefinedCapacityEntry
	{
		private static readonly ItemType[] DefaultAllowedItemTypes = {ItemType.All};
		private static readonly ToolType[] DefaultAllowedToolTypes = {ToolType.All};
		[Tooltip("Max item size allowed.")]
		public ItemSize MaxItemSize = ItemSize.None;

		[Tooltip("Item types allowed.")]
		public ItemType[] AllowedItemTypes;

		[Tooltip("Tool types allowed.")]
		public ToolType[] AllowedToolTypes;



		public bool CanFit(Pickupable toCheck)
		{
			if (toCheck == null) return false;
			ItemSize size = ItemSize.Huge;
			var itemAttrs = toCheck.GetComponent<ItemAttributes>();
			var tool = toCheck.GetComponent<Tool>();
			if (itemAttrs != null)
			{
				size = itemAttrs.size;
			}
			else
			{
				Logger.LogTraceFormat("{0} has no item attrs, defaulting to ItemSize.Huge", Category.Inventory, toCheck.name);
			}

			var sizeLimit = MaxItemSize;
			if (sizeLimit == ItemSize.None)
			{
				Logger.LogTraceFormat("No size restriction defined, defaulting to ItemSize.Huge", Category.Inventory);
				sizeLimit = ItemSize.Huge;
			}

			if (size > sizeLimit)
			{
				Logger.LogTraceFormat("{0} ({1}) exceeds max size of slot ({2})", Category.Inventory, toCheck.name, size, MaxItemSize);
				return false;
			}

			var allowedTypes = AllowedItemTypes;
			if (allowedTypes.Length == 0)
			{
				Logger.LogTraceFormat("No item types are defined in this slot. Defaulting to allowing all item types", Category.Inventory);
				allowedTypes = DefaultAllowedItemTypes;
			}

			//if ALL is in the list, don't check item type, proceed to tool type check
			if (!allowedTypes.Contains(ItemType.All))
			{
				ItemType type = ItemType.None;
				if (itemAttrs != null)
				{
					//type = itemAttrs.itemType;
				}
				else
				{
					Logger.LogTraceFormat("{0} has no item attrs, defaulting to ItemType.None", Category.Inventory, toCheck.name);
				}

				if (!allowedTypes.Contains(type))
				{
					Logger.LogTraceFormat("Item {0} with type {1} does not match allowed item types {2}.", Category.Inventory,
						toCheck.name, type, String.Join(",", allowedTypes.Select(ait => ait.ToString())));
					return false;
				}
			}

			var allowedTools = AllowedToolTypes;
			if (allowedTools.Length == 0)
			{
				Logger.LogTraceFormat("No tool types are defined in this slot. Defaulting to allowing all tool types", Category.Inventory);
				allowedTools = DefaultAllowedToolTypes;
			}


			//if ALL is in the list, don't check tool type
			if (!allowedTools.Contains(ToolType.All))
			{
				var toolType = ToolType.None;
				if (tool == null)
				{
					Logger.LogTraceFormat("{0} has no Tool, defaulting to ToolType.None", Category.Inventory, toCheck.name);
				}
				else
				{
					toolType = tool.ToolType;
				}

				if (!allowedTools.Contains(toolType))
				{
					Logger.LogTraceFormat("Item {0} with type {1} does not match allowed tool types {2}.", Category.Inventory,
						toCheck.name, toolType, String.Join(",", allowedTools.Select(ait => ait.ToString())));
					return false;
				}
			}

			return true;
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
	[ArrayElementTitle("NamedSlot")]
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