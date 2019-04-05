using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using Object = System.Object;

/// <summary>
/// Provides the general-purpose ability to create / destroy / colone objects and instances of prefabs, network synced, while using object pooling
/// for some kinds of objects. Even if the object is not pooled, this should still be used to spawn / despawn / clone it.
///
/// Notes on Spawn / Clone Hooks:
/// When an object is spawned in game, each component is broadcast OnSpawnedServer on the server and then OnSpawnedClient on the
/// client. This allows the component to set itself up after being spawned in a good "default" state that "just works".
/// What that entails is up to the object / component.
///
/// When an object is cloned in game, each component is broadcast OnClonedServer on the server and then OnClonedClient on
/// the client. This method also includes the game object that was cloned. This allows the component to
/// inspect the cloned object and change its own state to match the object. It's up to each component / object to
/// decide how to do this.
///
/// Neither of these will be called when spawning only a client-side object (via PoolClientInstantiate).
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
	/// properly implemented a OnSpawnedServer and OnSpawnedClient method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
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

		//fire the hooks for spawning
		tempObject.GetComponent<CustomNetTransform>()?.FireSpawnHooks();

		return tempObject;
	}

	/// <summary>
	/// Clone the item and ensures it is synced over the network. This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.
	/// </summary>
	/// <param name="toClone">GameObject to clone. This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.. Intended to work for any object, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which might need unique behavior for how they should spawn. If you are trying
	/// to clone something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented a OnClonedServer and OnClonedClient method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <returns>the newly created GameObject</returns>
	[Server]
	public static GameObject NetworkClone(GameObject toClone, Vector3? position = null, Transform parent = null, Quaternion? rotation = null)
	{
		if (!IsInstanceInit())
		{
			return null;
		}

		var prefab = DeterminePrefab(toClone);
		if (prefab == null)
		{
			Logger.LogErrorFormat("Object {0} at {1} cannot be cloned because it has no PoolPrefabTracker and its name" +
			                      " does not match a prefab name, so we cannot" +
			                      " determine the prefab to instantiate. Please fix this object so that it" +
			                      " has an attached PoolPrefabTracker or so its name matches the prefab it was created from.", Category.ItemSpawn, toClone.name, position);
		}

		GameObject tempObject = Instance.PoolInstantiate(prefab, position ?? TransformState.HiddenPos, rotation ?? Quaternion.identity, parent, out var isPooled);

		if (!isPooled)
		{
			NetworkServer.Spawn(tempObject);
			tempObject.GetComponent<CustomNetTransform>()?.NotifyPlayers();//Sending clientState for newly spawned items
		}

		//fire the hooks for cloning
		tempObject.GetComponent<CustomNetTransform>()?.FireCloneHooks(toClone);

		return tempObject;
	}

	/// <summary>
	/// Spawn the item and ensures it is synced over the network
	/// </summary>
	/// <param name="prefabName">Name of the prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented a OnSpawnedServer and OnSpawnedClient method.</param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
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
	/// Spawn the item locally without syncing it over the network. OnSpawnedServer and OnSpawnedClient hooks will
	/// not be called as this is only used for client side things.
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. </param>
	/// <param name="position">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
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

	private GameObject Clone(GameObject toClone, Vector3 position, Quaternion rotation, Transform parent)
	{
		GameObject tempObject = null;
		bool hide = position == TransformState.HiddenPos;
		//Cut off Z-axis
		Vector3 cleanPos = ( Vector2 ) position;
		Vector3 pos = hide ? TransformState.HiddenPos : cleanPos;
		tempObject = Instantiate(toClone, pos, rotation, parent);
		NetworkServer.Spawn(tempObject);
		tempObject.GetComponent<CustomNetTransform>()?.ReInitServerState();

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
			Logger.LogError("PoolManager was attempted to be used before it has initialized. Please delay using" +
			                " PoolManager (such as by using a coroutine to wait) until it is initialized. Nothing will" +
			                " be done and null will be returned.");
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
	/// 	If target has no ObjectBehavior or is not pooled, it will simply be deleted and the deletion
	/// 	will be synced to each client.
	/// </summary>
	[Server]
	public static void PoolNetworkDestroy(GameObject target)
	{
		if (!IsInstanceInit())
		{
			return;
		}

		//even if it has a pool prefab tracker, will still destroy it if it has no object behavior
		var poolPrefabTracker = target.GetComponent<PoolPrefabTracker>();
		var objBehavior = target.GetComponent<ObjectBehaviour>();
		if (poolPrefabTracker != null && objBehavior != null)
		{
			//pooled
			Instance.AddToPool(target);
			objBehavior.visibleState = false;
		}
		else
		{
			//not pooled
			NetworkServer.Destroy(target);
		}

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
	/// Tries to determine the prefab that was used to create the specified object.
	/// If there is an attached PoolPrefabTracker, uses that. Otherwise, uses the name
	/// and removes parentheses  like (Clone) or (1) to look up the prefab name in our map.
	/// </summary>
	/// <param name="instance">object whose prefab should be determined.</param>
	/// <returns>the prefab, otherwise null if it could not be determined.</returns>
	public static GameObject DeterminePrefab(GameObject instance)
	{
		var tracker = instance.GetComponent<PoolPrefabTracker>();
		if (tracker != null)
		{
			return tracker.myPrefab;
		}

		//regex below strips out parentheses and things between them
		var prefabName = Regex.Replace(instance.name, @"\(.*\)", "").Trim();

		return Instance.nameToSpawnablePrefab.ContainsKey(prefabName) ? GetPrefabByName(prefabName) : null;
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