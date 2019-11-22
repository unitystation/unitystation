
using UnityEngine;

/// <summary>
/// Defines how to populate an individual slot.
/// </summary>
public abstract class SlotPopulator : ScriptableObject
{
	/// <summary>
	/// Populate the specified slot.
	/// </summary>
	/// <param name="slot">slot to populate</param>
	/// <param name="context">context the slot is being populated in</param>
	public abstract void PopulateSlot(ItemSlot slot, PopulationContext context);
}
