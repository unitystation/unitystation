using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PoolManager : NetworkBehaviour
{
	public static PoolManager Instance;

	private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
	/*
	* Use this function for general purpose GameObject instantiation. It will instantiate the
	* a pooled instance immediately. If it doesn't find a pooled instance, it uses GetInstanceInactive()
	* to make a new one, and immediately instantiates and activates that. If the instance matches one already
	* in the pool (for example, one obtained from GetInstanceInactive), it just instantiates it.
	*/

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	/// <summary>
	///     For items that are network synced only!
	/// </summary>
	[Server]
	public GameObject PoolNetworkInstantiate(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent=null)
	{
		bool isPooled;

		GameObject tempObject = PoolInstantiate(prefab, position, rotation, parent, out isPooled);

		if (!isPooled)
		{
			NetworkServer.Spawn(tempObject);
		}
		var cnt = tempObject.GetComponent<CustomNetTransform>();
		if (cnt)
		{
			cnt.ReInitServerState();
		}
		return tempObject;
	}

	/// <summary>
	///     For non network stuff only! (e.g. bullets)
	/// </summary>
	public GameObject PoolClientInstantiate(GameObject prefab, Vector2 position, Quaternion rotation, 
		Transform parent=null)
	{
		bool isPooled; // not used for Client-only instantiation
		return PoolInstantiate(prefab, position, rotation, parent, out isPooled);
	}

	private GameObject PoolInstantiate(GameObject prefab, Vector2 position, Quaternion rotation, Transform parent, out bool pooledInstance)
	{
		GameObject tempObject = null;
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
				objBehaviour.visibleState = true;
			}

			tempObject.transform.position = position;
			tempObject.transform.rotation = rotation;
			tempObject.transform.localScale = prefab.transform.localScale;
			tempObject.transform.parent = parent;

			pooledInstance = true;
			
			return tempObject;
		}

		tempObject = Instantiate(prefab, position, rotation, parent);
		tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;

		pooledInstance = false;
		
		return tempObject;
	}

	private bool CanLoadFromPool(GameObject prefab)
	{
		return pools.ContainsKey(prefab) && pools[prefab].Count > 0;
	}

	[Server]
	public void PoolNetworkPreLoad(GameObject prefab)
	{
		GameObject tempObject = null;

		if (prefab == null)
		{
			return;
		}

		//pool for this prefab does not yet exist
		if (!pools.ContainsKey(prefab))
		{
			pools.Add(prefab, new List<GameObject>());
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
	public void PoolCacheObject(GameObject obj)
	{
		obj.AddComponent<PoolPrefabTracker>().myPrefab = obj;
		if (!pools.ContainsKey(obj))
		{
			pools.Add(obj, new List<GameObject>());
		}
	}

	/// <summary>
	///     For items that are network synced only!
	/// </summary>
	[Server]
	public void PoolNetworkDestroy(GameObject target)
	{
		AddToPool(target);
		target.GetComponent<ObjectBehaviour>().visibleState = false;
	}

	/// <summary>
	///     For non network stuff only! (e.g. bullets)
	/// </summary>
	public void PoolClientDestroy(GameObject target)
	{
		AddToPool(target);
		target.SetActive(false);
	}

	private void AddToPool(GameObject target)
	{
		var poolPrefabTracker = target.GetComponent<PoolPrefabTracker>();
		if ( !poolPrefabTracker )
		{
			Debug.LogWarning($"PoolPrefabTracker not found on {target}");
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
	public GameObject GetInstanceInactive(GameObject prefab)
	{
		GameObject tempObject = null;
		if (pools.ContainsKey(prefab))
		{
			if (pools[prefab].Count > 0)
			{
				int index = pools[prefab].Count - 1;
				tempObject = pools[prefab][index];
				return tempObject;
			}
		}
		else
		{
			pools.Add(prefab, new List<GameObject>());
		}

		tempObject = Instantiate(prefab);
		tempObject.SetActive(false);
		tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
		return tempObject;
	}

	public void ClearPool()
	{
		pools.Clear();
	}
}

//not used for clients unless it is a client side pool object only
public class PoolPrefabTracker : MonoBehaviour
{
	public GameObject myPrefab;
}