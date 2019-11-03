
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
	/// GameObject that was despawn.
	/// </summary>
	public readonly GameObject GameObject;

	/// <summary>
	/// SpawnInfo that was used to do the spawn.
	/// </summary>
	public readonly DespawnInfo DespawnInfo;

	private DespawnResult(DespawnInfo despawnInfo, GameObject gameObject, bool successful)
	{
		DespawnInfo = despawnInfo;
		GameObject = gameObject;
		Successful = successful;
	}

	/// <summary>
	/// Successfully despawned a single object
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public static DespawnResult Single(DespawnInfo spawnInfo, GameObject gameObject)
	{
		return new DespawnResult(spawnInfo, gameObject, true);
	}

	/// <summary>
	/// Failed to despawned.
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <returns></returns>
	public static DespawnResult Fail(DespawnInfo spawnInfo)
	{
		return new DespawnResult(spawnInfo, null, false);
	}

}
