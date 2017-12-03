using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class PoolManager : NetworkBehaviour
{

	public static Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
	private static PoolManager poolManager;

	public static PoolManager Instance {
		get {
			if (!poolManager) {
				poolManager = FindObjectOfType<PoolManager>();
			}
			return poolManager;
		}
	}
	/*
	* Use this function for general purpose GameObject instantiation. It will instantiate the
	* a pooled instance immediately. If it doesn't find a pooled instance, it uses GetInstanceInactive()
	* to make a new one, and immediately instantiates and activates that. If the instance matches one already
	* in the pool (for example, one obtained from GetInstanceInactive), it just instantiates it.
	*/

	void Awake()
	{
		pools = new Dictionary<GameObject, List<GameObject>>();
	}

	/// <summary>
	/// For items that are network synced only!
	/// </summary>
	[Server]
	static public GameObject PoolNetworkInstantiate(GameObject prefab, Vector2 position, Quaternion rotation)
	{
		GameObject tempObject = null;
		bool makeNew = false;
		if (pools.ContainsKey(prefab)) {
			if (pools[prefab].Count > 0) {
				//pool exists and has unused instances
				int index = pools[prefab].Count - 1;
				tempObject = pools[prefab][index];
				pools[prefab].RemoveAt(index);
				tempObject.GetComponent<ObjectBehaviour>().visibleState = true;
				tempObject.transform.position = position;
				tempObject.transform.rotation = rotation;
				tempObject.transform.localScale = prefab.transform.localScale;
				return tempObject;
			} else {
				//pool exists but is empty
				makeNew = true;
			}
		} else {
			//pool for this prefab does not yet exist
			pools.Add(prefab, new List<GameObject>());
			makeNew = true;
		}
		if (makeNew) {
			tempObject = (GameObject)Instantiate(prefab, position, rotation);
			tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
			NetworkServer.Spawn(tempObject);
			return tempObject;
		}
		return tempObject;
	}

	/// <summary>
	/// For non network stuff only! (e.g. bullets)
	/// </summary>
	static public GameObject PoolClientInstantiate(GameObject prefab, Vector2 position, Quaternion rotation)
	{
		GameObject tempObject = null;
		bool makeNew = false;
		if (pools.ContainsKey(prefab)) {
			if (pools[prefab].Count > 0) {
				//pool exists and has unused instances
				int index = pools[prefab].Count - 1;
				tempObject = pools[prefab][index];
				pools[prefab].RemoveAt(index);
				tempObject.SetActive(true);
				tempObject.transform.position = position;
				tempObject.transform.rotation = rotation;
				tempObject.transform.localScale = prefab.transform.localScale;
				return tempObject;
			} else {
				//pool exists but is empty
				makeNew = true;
			}
		} else {
			//pool for this prefab does not yet exist
			pools.Add(prefab, new List<GameObject>());
			makeNew = true;
		}
		if (makeNew) {
			tempObject = (GameObject)Instantiate(prefab, position, rotation);
			tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
			return tempObject;
		}
		return tempObject;
	}

	[Server]
	static public void PoolNetworkPreLoad(GameObject prefab)
	{
		GameObject tempObject = null;
		//pool for this prefab does not yet exist
		if (!pools.ContainsKey(prefab)) {
			pools.Add(prefab, new List<GameObject>());
		}
		tempObject = (GameObject)Instantiate(prefab, Vector2.zero, Quaternion.identity);
		tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
		NetworkServer.Spawn(tempObject);
		PoolNetworkDestroy(tempObject);
	}

	/// <summary>
	/// For items that are network synced only!
	/// </summary>
	[Server]
	static public void PoolNetworkDestroy(GameObject target)
	{
		GameObject prefab = target.GetComponent<PoolPrefabTracker>().myPrefab;
		prefab.transform.position = Vector2.zero;
		target.GetComponent<ObjectBehaviour>().visibleState = false;
		pools[prefab].Add(target);
	}

	/// <summary>
	/// For non network stuff only! (e.g. bullets)
	/// </summary>
	static public void PoolClientDestroy(GameObject target) {
		GameObject prefab = target.GetComponent<PoolPrefabTracker>().myPrefab;
		prefab.transform.position = Vector2.zero;
		target.SetActive(false);
		pools[prefab].Add(target);
	}

	/*
	* Use this function when you want to get an GameObject instance, but not enable it yet.
	* A good example would be when you want to pass information to the GameObject before it calls
	* OnEnable(). If it can't find a pooled instance, it creates and returns a new one. It does not
	* remove the instance from the pool. Note that it will always be enabled until the next frame, so OnEnable() will run.
	*/
	static public GameObject GetInstanceInactive(GameObject prefab)
	{
		GameObject tempObject = null;
		bool makeNew = false;
		if (pools.ContainsKey(prefab)) {
			if (pools[prefab].Count > 0) {
				int index = pools[prefab].Count - 1;
				tempObject = pools[prefab][index];
				return tempObject;
			} else {
				makeNew = true;
			}
		} else {
			pools.Add(prefab, new List<GameObject>());
			makeNew = true;
		}
		if (makeNew) {
			tempObject = (GameObject)Instantiate(prefab);
			tempObject.SetActive(false);
			tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
			return tempObject;
		}
		return tempObject;
	}

	public static void ClearPool()
	{
		pools.Clear();
	}
}

//not used for clients unless it is a client side pool object only
public class PoolPrefabTracker : MonoBehaviour
{
	public GameObject myPrefab;
}
