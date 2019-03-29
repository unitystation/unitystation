using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Provides the general-purpose ability to create instances of prefabs, network synced, while using object pooling.
/// </summary>
public class PoolManager : NetworkBehaviour
{
	public static PoolManager Instance;

	//dict for looking up spawnable prefabs (prefabs that can be used to instantiate new objects in the game) by name.
	//Name is basically the only thing that
	//is similar between client / server (instance ID is not) so we're going with this approach unless naming collisions
	//somehow become a problem, to allow clients to ask the server to spawn things (if they have permission)
	private Dictionary<String, GameObject> nameToSpawnablePrefab = new Dictionary<String, GameObject>();

	private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

	/// <summary>
	/// All gameobjects representing prefabs that can be spawned
	/// </summary>
	public static List<GameObject> SpawnablePrefabs => Instance.nameToSpawnablePrefab.Values.ToList();
	/*
	* Use this function for general purpose GameObject instantiation. It will instantiate the
	* a pooled instance immediately. If it doesn't find a pooled instance, it uses GetInstanceInactive()
	* to make a new one, and immediately instantiates and activates that. If the instance matches one already
	* in the pool (for example, one obtained from GetInstanceInactive), it just instantiates it.
	*/

	private void Awake()
	{
		//Search through our resources and find each prefab that has a CNT component
		var spawnablePrefabs = Resources.FindObjectsOfTypeAll<GameObject>()
			.Where(IsPrefab)
			.OrderBy(go => go.name)
			//check if they have CNTs (thus are spawnable)
			.Where(go => go.GetComponent<CustomNetTransform>() != null);

		foreach (var spawnablePrefab in spawnablePrefabs)
		{
			nameToSpawnablePrefab.Add(spawnablePrefab.name, spawnablePrefab);
		}

		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private static bool IsPrefab(GameObject toCheck) => !toCheck.transform.gameObject.scene.IsValid();

	/// <summary>
	/// Spawn the item and ensures it is synced over the network
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented a BeSpawned method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. THIS SHOULD RARELY BE NULL! Most things
	/// should always be spawned under the Objects transform in their matrix. However, many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned.</param>
	/// <returns>the newly created GameObject</returns>
	[Server]
	public static GameObject PoolNetworkInstantiate(GameObject prefab, Vector3? position = null, Transform parent = null, Quaternion? rotation = null)
	{
		if (!IsInstanceInit())
		{
			return null;
		}
		bool isPooled;

		GameObject tempObject = Instance.PoolInstantiate(prefab, position ?? TransformState.HiddenPos, rotation ?? Quaternion.identity, parent, out isPooled);

		if (!isPooled)
		{
			NetworkServer.Spawn(tempObject);
			tempObject.GetComponent<CustomNetTransform>()?.NotifyPlayers();//Sending clientState for newly spawned items
		}


		//broadcast BeSpawned so each component can set itself up
		tempObject.BroadcastMessage("BeSpawned", SendMessageOptions.DontRequireReceiver);

		return tempObject;
	}

	/// <summary>
	/// Spawn the item and ensures it is synced over the network
	/// </summary>
	/// <param name="prefabName">Name of the prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented a BeSpawned method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. THIS SHOULD RARELY BE NULL! Most things
	/// should always be spawned under the Objects transform in their matrix. However, many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned.</param>
	/// <returns>the newly created GameObject</returns>
	[Server]
	public static GameObject PoolNetworkInstantiate(String prefabName, Vector3? position = null, Transform parent = null, Quaternion? rotation = null)
	{
		GameObject prefab = GetPrefabByName(prefabName);
		if (prefab == null)
		{
			Logger.LogErrorFormat("Attempted to spawn prefab with name {0} which is either not an actual prefab name or" +
			                " is a prefab which is not spawnable. Request to spawn will be ignored.", Category.ItemSpawn, prefabName);
			return null;
		}

		return PoolNetworkInstantiate(prefab, position, parent, rotation);
	}

	/// <summary>
	/// Spawn the item locally without syncing it over the network.
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented a BeSpawned method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. THIS SHOULD RARELY BE NULL! Most things
	/// should always be spawned under the Objects transform in their matrix.</param>
	/// <returns>the newly created GameObject</returns>
	public static GameObject PoolClientInstantiate(GameObject prefab, Vector3? position = null, Transform parent = null, Quaternion? rotation = null)
	{
		if (!IsInstanceInit())
		{
			return null;
		}
		bool isPooled; // not used for Client-only instantiation
		return Instance.PoolInstantiate(prefab, position ?? TransformState.HiddenPos, rotation ?? Quaternion.identity, parent, out isPooled);
	}

	private GameObject PoolInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out bool pooledInstance)
	{
		GameObject tempObject = null;
		bool hide = position == TransformState.HiddenPos;
		//Cut off Z-axis
		Vector3 cleanPos = ( Vector2 ) position;
		Vector3 pos = hide ? TransformState.HiddenPos : cleanPos;
		if (CanLoadFromPool(prefab))
		{
			//pool exists and has unused instances
			int index = pools[prefab].Count - 1;
			tempObject = pools[prefab][index];
			pools[prefab].RemoveAt(index);
			tempObject.SetActive(true);

			ObjectBehaviour objBehaviour = tempObject.GetComponent<ObjectBehaviour>();
			if (objBehaviour)
			{
				objBehaviour.visibleState = !hide;
			}
			tempObject.transform.position = pos;
			tempObject.transform.rotation = rotation;
			tempObject.transform.localScale = prefab.transform.localScale;
			tempObject.transform.parent = parent;
			var cnt = tempObject.GetComponent<CustomNetTransform>();
			if ( cnt )
			{
				cnt.ReInitServerState();
				cnt.NotifyPlayers(); //Sending out clientState for already spawned items
			}

			pooledInstance = true;
		}
		else
		{
			tempObject = Instantiate(prefab, pos, rotation, parent);

			tempObject.GetComponent<CustomNetTransform>()?.ReInitServerState();

			tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;

			pooledInstance = false;
		}

		return tempObject;

	}

	private bool CanLoadFromPool(GameObject prefab)
	{
		return pools.ContainsKey(prefab) && pools[prefab].Count > 0;
	}

	private static bool IsInstanceInit()
	{
		if (Instance == null)
		{
			//TODO: What's the proper way to prevent this?
			Logger.LogError("PoolManager was attempted to be used before it has initialized. Please delay using" +
			                " PoolManager (such as by using a coroutine to wait) until it is initialized. Nothing will" +
			                " be initialized and null will be returned.");
			return false;
		}

		return true;
	}

	[Server]
	public static void PoolNetworkPreLoad(GameObject prefab)
	{
		if (!IsInstanceInit())
		{
			return;
		}
		GameObject tempObject = null;

		if (prefab == null)
		{
			return;
		}

		//pool for this prefab does not yet exist
		if (!Instance.pools.ContainsKey(prefab))
		{
			Instance.pools.Add(prefab, new List<GameObject>());
		}

		tempObject = Instantiate(prefab, Vector2.zero, Quaternion.identity);
		tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
		NetworkServer.Spawn(tempObject);
		PoolNetworkDestroy(tempObject);
	}

	/// <summary>
	///     For any objects placed in the scene prior to build
	///     that need to be added to the serverside object pool
	///     pass the object here and make sure object has a
	///     ObjectBehaviour component attached
	/// </summary>
	[Server]
	public static void PoolCacheObject(GameObject obj)
	{
		if (!IsInstanceInit())
		{
			return;
		}
		obj.AddComponent<PoolPrefabTracker>().myPrefab = obj;
		if (!Instance.pools.ContainsKey(obj))
		{
			Instance.pools.Add(obj, new List<GameObject>());
		}
	}

	/// <summary>
	///     For items that are network synced only!
	/// </summary>
	[Server]
	public static void PoolNetworkDestroy(GameObject target)
	{
		if (!IsInstanceInit())
		{
			return;
		}
		Instance.AddToPool(target);
		target.GetComponent<ObjectBehaviour>().visibleState = false;
	}

	/// <summary>
	///     For non network stuff only! (e.g. bullets)
	/// </summary>
	public static void PoolClientDestroy(GameObject target)
	{
		if (!IsInstanceInit())
		{
			return;
		}
		Instance.AddToPool(target);
		target.SetActive(false);
	}

	private void AddToPool(GameObject target)
	{
		var poolPrefabTracker = target.GetComponent<PoolPrefabTracker>();
		if ( !poolPrefabTracker )
		{
			Logger.LogWarning($"PoolPrefabTracker not found on {target}",Category.ItemSpawn);
			return;
		}
		GameObject prefab = poolPrefabTracker.myPrefab;
		prefab.transform.position = Vector2.zero;

		if (!pools.ContainsKey(prefab))
		{
			//pool for this prefab does not yet exist
			pools.Add(prefab, new List<GameObject>());
		}

		pools[prefab].Add(target);
	}

	/*
	* Use this function when you want to get an GameObject instance, but not enable it yet.
	* A good example would be when you want to pass information to the GameObject before it calls
	* OnEnable(). If it can't find a pooled instance, it creates and returns a new one. It does not
	* remove the instance from the pool. Note that it will always be enabled until the next frame, so OnEnable() will run.
	*/
	public static GameObject GetInstanceInactive(GameObject prefab)
	{
		if (!IsInstanceInit())
		{
			return null;
		}
		GameObject tempObject = null;
		if (Instance.pools.ContainsKey(prefab))
		{
			if (Instance.pools[prefab].Count > 0)
			{
				int index = Instance.pools[prefab].Count - 1;
				tempObject = Instance.pools[prefab][index];
				return tempObject;
			}
		}
		else
		{
			Instance.pools.Add(prefab, new List<GameObject>());
		}

		tempObject = Instantiate(prefab);
		tempObject.SetActive(false);
		tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
		return tempObject;
	}

	public static void ClearPool()
	{
		if (!IsInstanceInit())
		{
			return;
		}
		Instance.pools.Clear();
	}

	/// <summary>
	/// Gets a prefab by its name
	/// </summary>
	/// <param name="prefabName">name of the prefab</param>
	/// <returns>the gameobject of the prefab</returns>
	public static GameObject GetPrefabByName(string prefabName)
	{
		return Instance.nameToSpawnablePrefab[prefabName];
	}
}

//not used for clients unless it is a client side pool object only
public class PoolPrefabTracker : MonoBehaviour
{
	public GameObject myPrefab;
}