using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class PoolManager : MonoBehaviour {

	public static Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
	/*
	* Use this function for general purpose GameObject instantiation. It will instantiate the
	* a pooled instance immediately. If it doesn't find a pooled instance, it uses GetInstanceInactive()
	* to make a new one, and immediately instantiates and activates that. If the instance matches one already
	* in the pool (for example, one obtained from GetInstanceInactive), it just instantiates it.
	*/

	void Awake() {
		pools = new Dictionary<GameObject, List<GameObject>>();
	}

	static public GameObject PoolInstantiate(GameObject prefab, Vector2 position, Quaternion rotation) {
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

	static public void PoolDestroy(GameObject target) {
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
	static public GameObject GetInstanceInactive(GameObject prefab) {
		GameObject tempObject = null;
		bool makeNew = false;
		if (pools.ContainsKey(prefab)) {
			if (pools[prefab].Count > 0) {
				int index = pools[prefab].Count - 1;
				tempObject = pools[prefab][index];
				return tempObject;
			}
			else {
				makeNew = true;
			}
		}
		else {
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

	public static void ClearPool() {
		pools.Clear();
	}
}
public class PoolPrefabTracker : MonoBehaviour {
	public GameObject myPrefab;
}
