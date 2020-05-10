
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Encapsulates the object pool itself and all the objects in the pool.
/// Not thread safe.
/// </summary>
public class ObjectPool
{
	private readonly PoolConfig poolConfig;

	/// <summary>
	/// Map from prefab to the pooled instance of that prefab.
	/// </summary>
	private Dictionary<GameObject, List<GameObject>> prefabToPooledObjects;

	/// <summary>
	/// amount of objects currently in the pool
	/// </summary>
	private int size;

	/// <summary>
	/// Create the pool, using the specified configuration
	/// </summary>
	/// <param name="poolConfig"></param>
	public ObjectPool(PoolConfig poolConfig)
	{
		this.poolConfig = poolConfig;
		prefabToPooledObjects = new Dictionary<GameObject, List<GameObject>>();
	}


	/// <summary>
	/// Tries to get the object from pool if possible, removing it from the pool if
	/// successful.
	/// Only allows spawning networked objects if the game instance is a server.
	/// </summary>
	/// <param name="prefab">prefab to get an instance of</param>
	/// <param name="requireNonNetworked">if true, will only succeed if the object
	/// is not networked (lacks NetworkIdentity)</param>
	/// <param name="pooledObject">pooled object retrieved from pool, null if a pooled object
	/// could not be retrieved. Object will be inactive and at z level -100, may be in any state / 2d position
	/// though.</param>
	/// <returns>true iff successfully retrieved object from pool</returns>
	public bool TryGetFromPool(GameObject prefab, bool requireNonNetworked, out GameObject pooledObject)
	{
		pooledObject = null;
		if (prefab == null) return false;
		if (prefabToPooledObjects.ContainsKey(prefab) && prefabToPooledObjects[prefab].Count > 0)
		{
			//pool exists and has unused instances
			int index = prefabToPooledObjects[prefab].Count - 1;
			pooledObject = prefabToPooledObjects[prefab][index];
			bool isNetworked = pooledObject.GetComponent<NetworkIdentity>() != null;
			if (isNetworked && (requireNonNetworked || !CustomNetworkManager.IsServer))
			{
				Logger.LogWarningFormat("Attempted to get a networked object {0} from pool when" +
				                        " requireNonNetworked is true or this is a non-server instance of the game. Object will not be loaded from pool", Category.ItemSpawn,
					pooledObject);
				pooledObject = null;
				return false;
			}
			Logger.LogTraceFormat("Loading {0} from pool Pooled:{1} Index:{2}", Category.ItemSpawn, pooledObject.GetInstanceID(), prefabToPooledObjects[prefab].Count, index);
			prefabToPooledObjects[prefab].RemoveAt(index);
			return true;
		}

		return false;
	}


	/// <summary>
	/// Checks if instances of the indicated prefab can be pooled (regardless of
	/// current pool capacity)
	/// </summary>
	/// <param name="prefab">prefab to check</param>
	/// <returns>true iff instances of this prefab are allowed to be pooled</returns>
	public bool IsPoolable(GameObject prefab)
	{
		return poolConfig.IsPoolable(prefab);
	}

	/// <summary>
	/// Adds the object with the indicated prefab tracker component to the pool if
	/// the pool can accommodate it.
	/// </summary>
	/// <param name="poolPrefabTracker">prefab tracker component of the object
	/// to add to the pool</param>
	/// <returns>true if added to pool, false if cannot be added</returns>
	public bool TryAddToPool(PoolPrefabTracker poolPrefabTracker)
	{
		if (poolPrefabTracker == null || poolPrefabTracker.myPrefab == null) return false;
		if (!IsPoolable(poolPrefabTracker.myPrefab)) return false;

		var prefab = poolPrefabTracker.myPrefab;

		List<GameObject> pooledObjects;
		if (!prefabToPooledObjects.TryGetValue(prefab, out pooledObjects))
		{
			pooledObjects = new List<GameObject>();
			prefabToPooledObjects.Add(prefab, pooledObjects);
		}

		if (pooledObjects.Count < poolConfig.GetCapacity(prefab))
		{
			// we have capacity, add to pool
			pooledObjects.Add(poolPrefabTracker.gameObject);
			Logger.LogTraceFormat("Added {0} to pool, deactivated and moved to hiddenpos Pooled: {1} Index:{2}",
				Category.ItemSpawn, poolPrefabTracker.gameObject.GetInstanceID(), pooledObjects.Count, pooledObjects.Count-1);
			return true;
		}

		return false;
	}


	/// <summary>
	/// Removes everything from the pools, ensuring all objects in the pool are destroyed as well.
	/// </summary>
	public void Clear()
	{
		Logger.LogTrace("Clearing out object pools.", Category.ItemSpawn);
		foreach (var pooledObject in prefabToPooledObjects.Values.SelectMany(list => list))
		{
			// skip already destroyed objects
			if (!pooledObject) continue;

			var isNetworked = pooledObject.GetComponent<NetworkIdentity>() != null;
			// networked objects should only be destroyed on server side
			if (isNetworked && CustomNetworkManager.IsServer)
			{
				Logger.LogTraceFormat("Destroying networked object {0} from object pool.", Category.ItemSpawn, pooledObject);
				NetworkServer.Destroy(pooledObject);
			}
			else if (!isNetworked)
			{
				//non-networked objects should be destroyed on both sides
				Logger.LogTraceFormat("Destroying non-networked object {0} from object pool.", Category.ItemSpawn, pooledObject);
				Object.Destroy(pooledObject);
			}
			//note we ignore any networked objects on the clientside
		}

		foreach (var pooledList in prefabToPooledObjects.Values)
		{
			pooledList.Clear();
		}

		prefabToPooledObjects.Clear();
		Logger.LogTrace("Done clearing out object pools.", Category.ItemSpawn);
	}
}
