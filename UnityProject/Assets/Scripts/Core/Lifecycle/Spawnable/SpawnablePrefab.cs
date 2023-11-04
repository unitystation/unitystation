
using Items;
using Logs;
using Mirror;
using UnityEngine;

/// <summary>
/// Spawns an indicated prefab.
/// </summary>
public class SpawnablePrefab : ISpawnable, IClientSpawnable
{
	private readonly GameObject prefab;

	private SpawnablePrefab(GameObject prefab, bool PrePickRandom = false)
	{
		if (PrePickRandom)
		{
			if(prefab.TryGetComponent<RandomItemSpot>(out var randomItem)){
				randomItem.RollRandomPool(false);
				prefab = randomItem.spawnedItem;
			}
		}

		this.prefab = prefab;
	}

	/// <summary>
	/// Spawnable for spawning the indicated prefab
	/// </summary>
	/// <param name="prefab"></param>
	/// <returns></returns>
	public static SpawnablePrefab For(GameObject prefab, bool PrePickRandom = false)
	{
		return new SpawnablePrefab(prefab, PrePickRandom);
	}

	/// <summary>
	/// Spawnable for spawning the prefab with the given name
	/// </summary>
	/// <param name="prefabName"></param>
	/// <returns></returns>
	public static SpawnablePrefab For(string prefabName, bool PrePickRandom = false)
	{
		GameObject prefab = Spawn.GetPrefabByName(prefabName);
		if (prefab == null)
		{
			Loggy.LogErrorFormat("Attempted to spawn prefab with name {0} which is either not an actual prefab name or" +
			                      " is a prefab which is not spawnable. Request to spawn will be ignored.", Category.ItemSpawn, prefabName);
			return null;
		}
		return new SpawnablePrefab(prefab, PrePickRandom);
	}

	public SpawnableResult SpawnAt(SpawnDestination destination)
	{
		if (!SpawnableUtils.IsValidDestination(destination)) return SpawnableResult.Fail(destination);

		if (prefab == null)
		{
			Loggy.LogWarning("Cannot spawn, prefab to use is null", Category.ItemSpawn);
			return SpawnableResult.Fail(destination);
		}
		Loggy.LogTraceFormat("Spawning using prefab {0}", Category.ItemSpawn, prefab);

		if (Spawn._ObjectPool.TryPoolInstantiate(prefab, destination, false, out var spawnedObject))
		{
			return SpawnableResult.Single(spawnedObject, destination);
		}
		else
		{
			return SpawnableResult.Fail(destination);
		}
	}

	public SpawnableResult ClientSpawnAt(SpawnDestination destination)
	{
		if (Spawn._ObjectPool.TryPoolInstantiate(prefab, destination, true, out var spawnedObject))
		{
			return SpawnableResult.Single(spawnedObject, destination);
		}
		else
		{
			return SpawnableResult.Fail(destination);
		}
	}
}
