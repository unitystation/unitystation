
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Communicates the results of an attempt to spawn.
/// </summary>
public class SpawnResult
{
	/// <summary>
	/// True iff the spawn attempt was successful.
	/// </summary>
	public readonly bool Successful;

	/// <summary>
	/// GameObject that was spawned. If this was a multi-spawn
	/// (SpawnInfo.Count > 1), returns the first. Null if not successful
	/// </summary>
	public readonly GameObject GameObject;

	/// <summary>
	/// GameObjects that were spawned. Null if not successful
	/// </summary>
	public readonly IEnumerable<GameObject> GameObjects;

	/// <summary>
	/// SpawnInfo that was used to do the spawn.
	/// </summary>
	public readonly SpawnInfo SpawnInfo;

	private SpawnResult(SpawnInfo spawnInfo, GameObject gameObject, IEnumerable<GameObject> gameObjects, bool successful)
	{
		SpawnInfo = spawnInfo;
		GameObject = gameObject;
		GameObjects = gameObjects;
		Successful = successful;
	}

	/// <summary>
	/// Successfully spawned a single object
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <param name="gameObject"></param>
	/// <returns></returns>
	public static SpawnResult Single(SpawnInfo spawnInfo, GameObject gameObject)
	{
		return new SpawnResult(spawnInfo, gameObject, new[] {gameObject}, true);
	}

	/// <summary>
	/// Successfully spawned multiple objects
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <param name="gameObjects"></param>
	/// <returns></returns>
	public static SpawnResult Multiple(SpawnInfo spawnInfo, IEnumerable<GameObject> gameObjects)
	{
		var enumerable = gameObjects as GameObject[] ?? gameObjects.ToArray();
		return new SpawnResult(spawnInfo, enumerable.FirstOrDefault(), enumerable, true);
	}

	/// <summary>
	/// Failed to spawn.
	/// </summary>
	/// <param name="spawnInfo"></param>
	/// <returns></returns>
	public static SpawnResult Fail(SpawnInfo spawnInfo)
	{
		return new SpawnResult(spawnInfo, null, null, false);
	}

}
