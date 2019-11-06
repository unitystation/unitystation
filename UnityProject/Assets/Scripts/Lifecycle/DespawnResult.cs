
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Communicates the results of an attempt to despawn.
/// </summary>
public class DespawnResult
{
	/// <summary>
	/// True iff the despawn attempt was successful.
	/// </summary>
	public readonly bool Successful;

	/// <summary>
	/// GameObject that was despawned.
	/// </summary>
	public GameObject GameObject => DespawnInfo.GameObject;

	/// <summary>
	/// SpawnInfo that was used to do the spawn.
	/// </summary>
	public readonly DespawnInfo DespawnInfo;

	private DespawnResult(DespawnInfo despawnInfo, bool successful)
	{
		DespawnInfo = despawnInfo;
		Successful = successful;
	}

	/// <summary>
	/// Successfully despawned a single object
	/// </summary>
	/// <param name="despawnInfo"></param>
	/// <returns></returns>
	public static DespawnResult Single(DespawnInfo despawnInfo)
	{
		return new DespawnResult(despawnInfo, true);
	}

	/// <summary>
	/// Failed to despawned.
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <returns></returns>
	public static DespawnResult Fail(DespawnInfo spawnInfo)
	{
		return new DespawnResult(spawnInfo, false);
	}

}
