
using UnityEngine;

/// <summary>
/// SO which describes how to spawn a particular object on the server. Can also be used to spawn something into an inventory
/// slot (if it is allowed to go in inventory).
/// </summary>
public abstract class Spawnable : SlotPopulator, ISpawnable
{
	public override void PopulateSlot(ItemSlot slot, PopulationContext context)
	{
		if (slot.Item != null)
		{
			Logger.LogTraceFormat("Skipping populating slot {0} because it already has an item.",
				Category.Inventory, slot);
			return;
		}

		Logger.LogTraceFormat("Populating {0} using spawnable {1}", Category.Inventory, slot, name);

		//spawn it at hidden pos
		var result = SpawnAt(SpawnDestination.HiddenPos());
		if (result.Successful && result.IsSingle)
		{
			if (result.GameObject.GetComponent<Pickupable>() == null)
			{
				Logger.LogTraceFormat("Cannot use this spawnable {0} to populate slot as the spawned object {1}" +
				                      " is not pickupable.", Category.Inventory, name, result.GameObject);
				return;
			}
			//move it into inventory
			Inventory.ServerAdd(result.GameObject, slot);
			Logger.LogTraceFormat("Populated {0}", Category.Inventory, slot);
		}

	}

	public SpawnableResult SpawnAt(SpawnDestination destination)
	{
		if (destination == null)
		{
			Logger.LogError("Cannot spawn, destination is null", Category.ItemSpawn);
			return SpawnableResult.Fail(destination);
		}
		if (destination.CancelIfImpassable)
		{
			if (SpawnDestination.IsTotallyImpassable(destination.WorldPosition.CutToInt()))
			{
				Logger.LogTraceFormat("Cancelling spawn of {0} because" +
				                      " the position being spawned to {1} is impassable",
					Category.ItemSpawn, name, destination.WorldPosition.CutToInt());
				return SpawnableResult.Fail(destination);
			}
		}

		return SpawnIt(destination);
	}


	/// <summary>
	/// Spawn something at the indicated destination on the server
	/// </summary>
	/// <param name="destination">description of where it should be spawned.</param>
	/// <returns>result of attempting to spawn</returns>
	public abstract SpawnableResult SpawnIt(SpawnDestination destination);
}
