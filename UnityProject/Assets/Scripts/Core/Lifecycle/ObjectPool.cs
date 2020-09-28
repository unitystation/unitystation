
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Encapsulates the object pool itself and all the objects in the pool.
/// Not thread safe. This entire class is only intended to be used internally by lifecycle
/// system, it should generally not be used directly by object components.
/// </summary>
public class ObjectPool
{
	private readonly PoolConfig poolConfig;

	/// <summary>
	/// Map from prefab to the pooled instance of that prefab.
	/// </summary>
	private Dictionary<GameObject, Stack<GameObject>> prefabToPooledObjects;

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
		prefabToPooledObjects = new Dictionary<GameObject, Stack<GameObject>>();
	}

	/// <summary>
	/// Tries to add the object to the pool. If object can fit in the pool, moves it
	/// to hiddenpos and deactivates it. Otherwise, just destroys it.
	/// In all cases, the object will be destroyed on the client side.
	///
	/// Only allows despawning of a networked object if this is a server instance of the game.
	///
	/// Does nothing if object is networked and this is called as a client (because
	/// client shouldn't initiate despawning of a networked object - only the server should)
	/// </summary>
	/// <param name="target">object to try to despawn</param>
	/// <param name="asClient">indicates this is being called from client-side logic.
	/// This will ensure the object is despawned only if it is non-networked.
	/// </param>
	/// <returns>true iff successful at despawning it or object is already despawned.</returns>
	public bool TryDespawnToPool(GameObject target, bool asClient)
	{
		if (!target || !target.activeSelf)
		{
			Logger.LogTraceFormat("Object {0} already destroyed or inactive (thus already in the pool), so" +
			                      " ignoring this attempt to despawn it to the pool.", Category.ItemSpawn, target);
			// it's allowed to call this method - in this situation...sometimes a component may not know that the object is
			// already despawned. So we return success.
			return true;
		}
		var isNetworked = target.GetComponent<NetworkIdentity>() != null;
		if (isNetworked && (asClient || !CustomNetworkManager.IsServer))
		{
			Logger.LogWarningFormat("Tried to despawn networked object {0} from clientside logic or" +
			                        " as a non-server instance of the game," +
			                        " object will not be despawned.", Category.ItemSpawn, target);
			return false;
		}
		var poolPrefabTracker = target.GetComponent<PoolPrefabTracker>();
		var shouldDestroy = false;
		if (!poolPrefabTracker)
		{
			// this is only needed at trace level because most mapped items don't have a prefab tracker
			Logger.LogTraceFormat("PoolPrefabTracker not found on {0}, destroying it", Category.ItemSpawn, target);
			shouldDestroy = true;
		}

		// going to add it to the pool
		if (!shouldDestroy && TryAddToPool(poolPrefabTracker))
		{
			//just a failsafe, not sure this is strictly necessary.
			poolPrefabTracker.myPrefab.transform.position = Vector2.zero;

			if (isNetworked)
			{
				// failsafe - we should be able to assume we are server if this code path is reached
				if (!CustomNetworkManager.IsServer)
				{
					Logger.LogErrorFormat("Coding error! Tried to add networked object {0} to pool but we " +
					                      "are not server.", Category.ItemSpawn, target);
				}
				//destroy for all clients, keep only in the server pool
				NetworkServer.UnSpawn(target);

				//transform.VisibleState seems to be valid only on server side, so we make it invisible
				//here when we're going to add it to the pool, but we don't do that on clientside.
				if (target.TryGetComponent<IPushable>(out var pushable))
				{
					pushable.VisibleState = false;
				}

				if (target.TryGetComponent<CustomNetTransform>(out var cnt))
				{
					cnt.DisappearFromWorldServer();
				}
				else
				{
					// no CNT - this is typically the case for non-networked objects.
					// in this case we just manually move it to hiddenpos.
					target.transform.position = TransformState.HiddenPos;
				}


			}
			else
			{
				if (target.TryGetComponent<CustomNetTransform>(out var cnt))
				{
					cnt.DisappearFromWorld();
				}
				else
				{
					// no CNT - this is typically the case for non-networked objects.
					// in this case we just manually move it to hiddenpos.
					target.transform.position = TransformState.HiddenPos;
				}
			}

			//regardless of what happens we deactivate it when it goes into the pool
			target.SetActive(false);
			return true;
		}
		else
		{
			//gonna destroy it
			if (isNetworked)
			{
				// failsafe - we should be able to assume we are server if this code path is reached
				if (!CustomNetworkManager.IsServer)
				{
					Logger.LogErrorFormat("Coding error! Tried to despawn networked object {0} but we " +
					                      "are not server.", Category.ItemSpawn, target);
				}
				//destroy for everyone
				NetworkServer.Destroy(target);
			}
			else
			{
				// destroy only for this instance of the game
				Object.Destroy(target);
			}

			return true;
		}
	}

	/// <summary>
	/// Tries to instantiate an object, getting it from the pool if possible.
	///
	/// If called as server, will spawn the object for all clients if the object is networked. If called as client,
	/// will only spawn the object for that client (trying to spawn networked object
	/// as client will result in nothing happening).
	/// Instantiates the prefab at the specified location, taking from the pool if possible.
	/// Does not call any hooks. Object will always be newly-created for all clients, as clients
	/// have no pool for networked objects.
	/// </summary>
	/// <param name="prefab">prefab to instantiate</param>
	/// <param name="destination">destination to spawn</param>
	/// <param name="asClient">indicates this is being called from client-side logic.
	/// This will ensure the object is spawned only if it is non-networked.
	/// </param>
	/// <param name="spawnedObject">spawned object, null if unsuccessful</param>
	/// <returns>true iff successful</returns>
	public bool TryPoolInstantiate(GameObject prefab, SpawnDestination destination, bool asClient, out GameObject spawnedObject)
	{
		bool hide = destination.WorldPosition == TransformState.HiddenPos;
		//Cut off Z-axis
		Vector3 cleanPos = ( Vector2 ) destination.WorldPosition;
		Vector3 pos = hide ? TransformState.HiddenPos : cleanPos;
		bool isNetworked;
		if (TryGetFromPool(prefab, asClient, out spawnedObject))
		{
			isNetworked = spawnedObject.GetComponent<NetworkIdentity>() != null;
			spawnedObject.SetActive(true);
			spawnedObject.transform.position = pos;
			spawnedObject.transform.localRotation = destination.LocalRotation;
			spawnedObject.transform.localScale = prefab.transform.localScale;
			spawnedObject.transform.parent = destination.Parent;
			if ( spawnedObject.TryGetComponent<CustomNetTransform>(out var cnt) )
			{
				cnt.ReInitServerState();
				cnt.NotifyPlayers(); //Sending out clientState for already spawned items
			}
		}
		else
		{
			spawnedObject = Object.Instantiate(prefab, pos, destination.Parent.rotation * destination.LocalRotation, destination.Parent);
			isNetworked = spawnedObject.GetComponent<NetworkIdentity>() != null;
			if (isNetworked && (asClient || !CustomNetworkManager.IsServer))
			{
				Logger.LogWarningFormat("Attempted to spawn a networked object {0} as a client or from a non-server instance" +
				                        " of the game. Object will not be spawned", Category.ItemSpawn,
					spawnedObject);
				Object.Destroy(spawnedObject);
				spawnedObject = null;
				return false;
			}
			spawnedObject.name = prefab.name;
			spawnedObject.GetComponent<CustomNetTransform>()?.ReInitServerState();
			// only add pool prefab tracker if the object can be pooled
			if (IsPoolable(prefab))
			{
				spawnedObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
			}
		}

		if (isNetworked)
		{
			// failsafe - we should be able to assume we are server if this code path is reached
			if (!CustomNetworkManager.IsServer)
			{
				Logger.LogErrorFormat("Coding error! Tried to spawn networked object {0} but we " +
				                      "are not server.", Category.ItemSpawn, spawnedObject);
			}

			// regardless of whether it was from the pool or not, we know it doesn't exist clientside
			// because there's no clientside pool for networked objects, so
			// we only need to tell client to spawn it
			NetworkServer.Spawn(spawnedObject);
			spawnedObject.GetComponent<CustomNetTransform>()
				?.NotifyPlayers(); //Sending clientState for newly spawned items
		}

		return spawnedObject;
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
	private bool TryGetFromPool(GameObject prefab, bool requireNonNetworked, out GameObject pooledObject)
	{
		pooledObject = null;
		if (prefab == null) return false;
		if (prefabToPooledObjects.ContainsKey(prefab) && prefabToPooledObjects[prefab].Count > 0)
		{
			//pool exists and has unused instances
			pooledObject = prefabToPooledObjects[prefab].Peek();
			if (!pooledObject)
			{
				Logger.LogErrorFormat("Coding error! Tried to get {0} from pool but it's already been destroyed." +
				                      " Destroyed objects should not be in the pool", Category.ItemSpawn, pooledObject);
				pooledObject = null;
				return false;
			}
			bool isNetworked = pooledObject.GetComponent<NetworkIdentity>() != null;
			if (isNetworked && (requireNonNetworked || !CustomNetworkManager.IsServer))
			{
				Logger.LogWarningFormat("Attempted to get a networked object {0} from pool when" +
				                        " requireNonNetworked is true or this is a non-server instance of the game. Object will not be loaded from pool", Category.ItemSpawn,
					pooledObject);
				pooledObject = null;
				return false;
			}
			Logger.LogTraceFormat("Loading {0} from pool Pooled:{1}", Category.ItemSpawn, pooledObject.GetInstanceID(), prefabToPooledObjects[prefab].Count);
			prefabToPooledObjects[prefab].Pop();
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
	private bool IsPoolable(GameObject prefab)
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
	private bool TryAddToPool(PoolPrefabTracker poolPrefabTracker)
	{
		if (poolPrefabTracker == null || poolPrefabTracker.myPrefab == null) return false;
		if (!IsPoolable(poolPrefabTracker.myPrefab)) return false;

		var prefab = poolPrefabTracker.myPrefab;

		Stack<GameObject> pooledObjects;
		if (!prefabToPooledObjects.TryGetValue(prefab, out pooledObjects))
		{
			pooledObjects = new Stack<GameObject>();
			prefabToPooledObjects.Add(prefab, pooledObjects);
		}

		if (pooledObjects.Count < poolConfig.GetCapacity(prefab))
		{
			// we have capacity, add to pool
			pooledObjects.Push(poolPrefabTracker.gameObject);
			Logger.LogTraceFormat("Added {0} to pool, deactivated and moved to hiddenpos Pooled: {1}",
				Category.ItemSpawn, poolPrefabTracker.gameObject.GetInstanceID(), pooledObjects.Count);
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
			if (isNetworked)
			{
				if (CustomNetworkManager.IsServer)
				{
					Logger.LogTraceFormat("Destroying networked object {0} from object pool.", Category.ItemSpawn, pooledObject);
					NetworkServer.Destroy(pooledObject);
				}
				else
				{
					Logger.LogErrorFormat("Coding error! Found networked object {0} in clientside pool." +
					                      " Networked objects should not be in the clientside pool.", Category.ItemSpawn);
				}
			}
			else
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
