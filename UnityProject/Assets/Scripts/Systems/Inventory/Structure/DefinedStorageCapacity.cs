
using System;
using System.Linq;
using Logs;
using UnityEngine;

/// <summary>
/// ItemStorageCapacity which should cover most typical use cases, allowing you to define all the aspects of
/// what is allowed.
/// </summary>
[CreateAssetMenu(fileName = "DefinedStorageCapacity", menuName = "Inventory/Structure/DefinedStorageCapacity", order = 4)]
public class DefinedStorageCapacity : ItemStorageCapacity
{
	[System.Serializable]
	public class NamedDefinedCapacityEntry
	{
		[Tooltip("Slot this is for")]
		public NamedSlot NamedSlot;
		[Tooltip("Capacity of the indicated named slot.")]
		public SlotCapacity Capacity;
	}

	[SerializeField]
	[Tooltip("Capacity of all indexed slots.")]
	private SlotCapacity IndexedSlotCapacity = null;

	[SerializeField]
	[Tooltip("Capacity capabilities of each named slot")]
	[ArrayElementTitle("NamedSlot")]
	private NamedDefinedCapacityEntry[] NamedSlotCapacity = null;

	public override bool CanFit(Pickupable toCheck, SlotIdentifier inSlot)
	{
		Loggy.LogTraceFormat("Checking if {0} can fit in {1}", Category.Inventory, toCheck.name, inSlot);
		//which type of slot are we checking
		if (inSlot.SlotIdentifierType == SlotIdentifierType.Indexed)
		{
			if (IndexedSlotCapacity == null)
			{
				Loggy.LogTrace("Indexed slot capacity not defined. Defaulting to no fit.", Category.Inventory);
				return false;
			}
			return IndexedSlotCapacity.CanFit(toCheck);
		}
		else
		{
			NamedDefinedCapacityEntry entry = NamedSlotCapacity.FirstOrDefault(nsc => nsc.NamedSlot == inSlot.NamedSlot);
			if (entry == null || entry.Capacity == null)
			{
				Loggy.LogTraceFormat("Slot capacity not defined for {0}. Defaulting to no fit.", Category.Inventory, inSlot.NamedSlot);
				return false;
			}

			return entry.Capacity.CanFit(toCheck);
		}
	}
}