
using Mirror;
using UnityEngine;

/// <summary>
/// Spawnable capable of spawning a clone of a particular game object
/// </summary>
public class SpawnableClone : ISpawnable
{
	private readonly GameObject toClone;

	private SpawnableClone(GameObject clone)
	{
		toClone = clone;
	}

	/// <summary>
	/// Spawnable which can spawn a clone of the specified game object
	/// </summary>
	/// <param name="toClone"></param>
	/// <returns></returns>
	public static SpawnableClone Of(GameObject toClone)
	{
		return new SpawnableClone(toClone);
	}

	public SpawnableResult SpawnAt(SpawnDestination destination)
	{
		var prefab = Spawn.DeterminePrefab(toClone);
		if (prefab == null)
		{
			Logger.LogErrorFormat(
				"Object {0} cannot be cloned because it has no PoolPrefabTracker and its name" +
				" does not match a prefab name, so we cannot" +
				" determine the prefab to instantiate. Please fix this object so that it" +
				" has an attached PoolPrefabTracker or so its name matches the prefab it was created from.",
				Category.ItemSpawn, toClone);
			return SpawnableResult.Fail(destination);
		}

		if (Spawn._ObjectPool.TryPoolInstantiate(prefab, destination, false, out var spawnedObject))
		{
			return SpawnableResult.Single(spawnedObject, destination);
		}
		else
		{
			return SpawnableResult.Fail(destination);
		}
	}
}
