
using Logs;
using UnityEngine;

/// <summary>
/// SO which describes how to spawn something on the server.
/// </summary>
public static class SpawnableUtils
{

	/// <summary>
	/// Validates that destination not null and location is passable (if destination.CancelIfImpassable)
	/// </summary>
	/// <param name="destination"></param>
	/// <returns></returns>
	public static bool IsValidDestination(SpawnDestination destination)
	{
		if (destination == null)
		{
			Loggy.LogError("Cannot spawn, destination is null", Category.ItemSpawn);
			return false;
		}
		if (destination.CancelIfImpassable)
		{
			if (SpawnDestination.IsTotallyImpassable(destination.WorldPosition.CutToInt()))
			{
				Loggy.LogTraceFormat("Cancelling spawn because" +
				                      " the position being spawned to {0} is impassable",
					Category.ItemSpawn, destination.WorldPosition.CutToInt());
				return false;
			}
		}

		return true;
	}
}
