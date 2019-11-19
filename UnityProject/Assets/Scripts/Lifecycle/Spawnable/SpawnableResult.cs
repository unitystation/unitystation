
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Communicates the results of attempting to spawn a spawnable.
/// </summary>
public class SpawnableResult
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
	/// Destination that was attempted to be spawned at.
	/// </summary>
	public readonly SpawnDestination SpawnDestination;

	private SpawnableResult(bool successful, GameObject gameObject, SpawnDestination spawnDestination)
	{
		Successful = successful;
		GameObject = gameObject;
		SpawnDestination = spawnDestination;
	}

	/// <summary>
	/// Spawning was successful
	/// </summary>
	/// <param name="spawned">object that was newly spawned</param>
	/// <param name="destination">destination the object was spawned at</param>
	/// <returns></returns>
	public static SpawnableResult Success(GameObject spawned, SpawnDestination destination)
	{
		return new SpawnableResult(true, spawned, destination);
	}

	/// <summary>
	/// Spawning was not successful
	/// </summary>
	/// <param name="destination">destination attempting to be spawned at</param>
	/// <returns></returns>
	public static SpawnableResult Fail(SpawnDestination destination)
	{
		return new SpawnableResult(false, null, destination);
	}
}
