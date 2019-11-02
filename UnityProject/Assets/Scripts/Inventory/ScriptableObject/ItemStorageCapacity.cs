
using UnityEngine;

/// <summary>
/// Defines what items are allowed to fit in a given ItemStorage's item slot.
/// </summary>
public abstract class ItemStorageCapacity : ScriptableObject
{

	/// <summary>
	/// Check if the given item is able to fit in the specified slot (regardless of whether it is occupied)
	/// </summary>
	/// <param name="toCheck"></param>
	/// <param name="inSlot"></param>
	/// <returns>true iff the item is able to fit</returns>
	public abstract bool CanFit(Pickupable toCheck, ItemSlot inSlot);
}
