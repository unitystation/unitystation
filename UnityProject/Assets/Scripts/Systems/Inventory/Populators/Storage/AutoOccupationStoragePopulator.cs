
using UnityEngine;

/// <summary>
/// Populator which only works when being used to spawn a player (when not spawning naked).
/// Populates the starting inventory of the player based on their occupation - uses the populator
/// defined in that Occupation, then the standard loadout
/// </summary>
[CreateAssetMenu(fileName = "AutoOccupationStoragePopulator", menuName = "Inventory/Populators/Storage/AutoOccupationStoragePopulator")]
public class AutoOccupationStoragePopulator : ItemStoragePopulator
{
	[SerializeField]
	[Tooltip("Populator to use after the occupation-specific populator has been run.")]
	private ItemStoragePopulator StandardPopulator = null;

	public override void PopulateItemStorage(ItemStorage toPopulate, PopulationContext context)
	{
		var occupation = PopulatorUtils.TryGetOccupation(context);
		if (occupation == null) return;
		if (context.SpawnInfo.SpawnItems == false) return;

		Logger.LogTraceFormat("Populating item storage using configured populator for occupation {0}",
			Category.Inventory, occupation.JobType);

		occupation.InventoryPopulator.PopulateItemStorage(toPopulate, context);
		if (StandardPopulator != null)
		{
			Logger.LogTraceFormat("Populating item storage using standard populator",
				Category.Inventory);
			StandardPopulator.PopulateItemStorage(toPopulate, context);
		}
	}
}
