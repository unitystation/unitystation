
using UnityEngine;

/// <summary>
/// Defines which items can fit in a particular slot. Can also be used itself as
/// a storage capacity, simply defines a global capacity for all slots in the
/// storage.
/// </summary>
public abstract class SlotCapacity : ItemStorageCapacity
{
	/// <summary>
	/// Check if the given item is able to fit in this slot (regardless of whether it is occupied)
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="inSlot"></param>
	/// <returns>true iff the item is able to fit</returns>
	public abstract bool CanFit(Pickupable toCheck);

	public override bool CanFit(Pickupable toCheck, SlotIdentifier inSlot)
	{
		return CanFit(toCheck);
	}
}
