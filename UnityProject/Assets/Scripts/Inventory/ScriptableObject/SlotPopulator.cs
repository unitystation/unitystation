
using UnityEngine;

/// <summary>
/// Defines how to populate an individual slot.
/// </summary>
public abstract class SlotPopulator : ScriptableObject
{
	/// <summary>
	/// Populate the specified slot.
	/// </summary>
	/// <param name="toPopulate">slot to populate</param>
	public abstract void PopulateSlot(ItemSlot toPopulate);
}
