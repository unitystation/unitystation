
using Mirror;
using UnityEngine;

/// <summary>
/// Spawns an indicated prefab.
/// </summary>
public class SpawnablePrefab : ISpawnable, IClientSpawnable
{
	private readonly GameObject prefab;

	private SpawnablePrefab(GameObject prefab)
	{
		this.prefab = prefab;
	}

	/// <summary>
	/// Spawnable for spawning the indicated prefab
	/// </summary>
	/// <param name="prefab"></param>
	/// <returns></returns>
	public static SpawnablePrefab For(GameObject prefab)
	{
		return new SpawnablePrefab(prefab);
	}

	/// <summary>
	/// Spawnable for spawning the prefab with the given name
	/// </summary>
	/// <param name="prefabName"></param>
	/// <returns></returns>
	public static SpawnablePrefab For(string prefabName)
	{
		GameObject prefab = Spawn.GetPrefabByName(prefabName);
		if (prefab == null)
		{
			Logger.LogErrorFormat("Attempted to spawn prefab with name {0} which is either not an actual prefab name or" +
			                      " is a prefab which is not spawnable. Request to spawn will be ignored.", Category.ItemSpawn, prefabName);
			return null;
		}
		return new SpawnablePrefab(prefab);
	}

	public SpawnableResult SpawnAt(SpawnDestination destination)
	{
		if (!SpawnableUtils.IsValidDestination(destination)) return SpawnableResult.Fail(destination);

		if (prefab == null)
		{
			Logger.LogError("Cannot spawn, prefab to use is null", Category.ItemSpawn);
			return SpawnableResult.Fail(destination);
		}
		Logger.LogTraceFormat("Spawning using prefab {0}", Category.ItemSpawn, prefab);

		bool isPooled;

		GameObject tempObject = Spawn._PoolInstantiate(prefab, destination,
			out isPooled);

		if (!isPooled)
		{
			Logger.LogTrace("Prefab to spawn was not pooled, spawning new instance.", Category.ItemSpawn);
			NetworkServer.Spawn(tempObject);
			tempObject.GetComponent<CustomNetTransform>()
				?.NotifyPlayers(); //Sending clientState for newly spawned items
		}
		else
		{
			Logger.LogTrace("Prefab to spawn was pooled, reusing it...", Category.ItemSpawn);
		}

		return SpawnableResult.Single(tempObject, destination);
	}

	public SpawnableResult ClientSpawnAt(SpawnDestination destination)
	{
		bool isPooled; // not used for Client-only instantiation
		var go = Spawn._PoolInstantiate(prefab, destination, out isPooled);

		return SpawnableResult.Single(go, destination);
	}
}
