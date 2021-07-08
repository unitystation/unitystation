
using UnityEngine;

/// <summary>
/// Provides details / context in which item storage population is being performed so it can be leveraged
/// by populators.
/// </summary>
public class PopulationContext
{
	/// <summary>
	/// If the population is being done as part of a spawning process, provides the info
	/// for the spawn.
	/// </summary>
	public readonly SpawnInfo SpawnInfo;

	private PopulationContext(SpawnInfo spawnInfo)
	{
		SpawnInfo = spawnInfo;
	}

	/// <summary>
	/// Population is occurring as the result of a spawn
	/// </summary>
	/// <param name="info"></param>
	/// <param name="spawnedObject">object that was spawned.</param>
	/// <returns></returns>
	public static PopulationContext AfterSpawn(SpawnInfo info)
	{
		return new PopulationContext(info);
	}
}
