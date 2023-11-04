
using System.Collections.Generic;
using System.Linq;
using Logs;
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
	/// GameObject that was spawned. If this was a multi-spawn,
	/// returns the first. Null if not successful
	/// </summary>
	public readonly GameObject GameObject;

	/// <summary>
	/// GameObjects that were spawned. Null if not successful
	/// </summary>
	public readonly IEnumerable<GameObject> GameObjects;

	/// <summary>
	/// Destination that was attempted to be spawned at.
	/// </summary>
	public readonly SpawnDestination SpawnDestination;


	/// <summary>
	/// True iff this was only a single spawned object
	/// </summary>
	public bool IsSingle => GameObjects.Count() <= 1;

	/// <summary>
	/// True iff this was for spawning multiple objects.
	/// </summary>
	public bool IsMultiple => !IsSingle;

	private SpawnableResult(bool successful, GameObject gameObject,
		IEnumerable<GameObject> gameObjects, SpawnDestination spawnDestination)
	{
		Successful = successful;
		GameObject = gameObject;
		SpawnDestination = spawnDestination;
		GameObjects = gameObjects;
	}


	/// <summary>
	/// Spawning single object was successful
	/// </summary>
	/// <param name="spawned">object that was newly spawned</param>
	/// <param name="destination">destination the object was spawned at</param>
	/// <returns></returns>
	public static SpawnableResult Single(GameObject spawned, SpawnDestination destination)
	{
		return new SpawnableResult(true, spawned, new [] { spawned },
			destination);
	}

	/// <summary>
	/// Spawning multiple objects was successful
	/// </summary>
	/// <param name="spawned">objects that were newly spawned</param>
	/// <param name="destination">destination the objects were spawned at</param>
	/// <returns></returns>
	public static SpawnableResult Multiple(IEnumerable<GameObject> spawned, SpawnDestination destination)
	{
		var gameObjects = spawned as GameObject[] ?? spawned.ToArray();
		if (gameObjects.Length == 0)
		{
			Loggy.LogWarningFormat("SpawnableResult of Multiple objects has nothing to spawn at worldPos {0}", Category.ItemSpawn,
				destination.WorldPosition);
			return Fail(destination);
		}
		return new SpawnableResult(true, gameObjects.First(),
			gameObjects,
			destination);
	}

	/// <summary>
	/// Spawning was not successful
	/// </summary>
	/// <param name="destination">destination attempting to be spawned at</param>
	/// <returns></returns>
	public static SpawnableResult Fail(SpawnDestination destination)
	{
		return new SpawnableResult(false, null, null, destination);
	}
}
