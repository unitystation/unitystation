
using UnityEngine;

/// <summary>
/// base SO for item storage populators, which define how an ItemStorage should be populated.
/// </summary>
public abstract class ItemStoragePopulator : ScriptableObject, IItemStoragePopulator
{

	/// <summary>
	/// Populate the specified storage.
	/// </summary>
	/// <param name="toPopulate">storage to populate</param>
	public abstract void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context, SpawnInfo info);
}
