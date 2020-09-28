
using UnityEngine;

/// <summary>
/// Defines what items are allowed to fit in a particular ItemStorage's item slot.
/// </summary>
public abstract class ItemStorageCapacity : ScriptableObject
{

	/// <summary>
	/// Check if the given item is able to fit in the specified slot on this item storage
	/// (regardless of whether it is occupied)
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="inSlot">slot on this item storage that we are seeing if
	/// toCheck fits in</param>
	/// <returns>true iff the item is able to fit</returns>
	public abstract bool CanFit(Pickupable toCheck, SlotIdentifier inSlot);
}
