using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PoolManager : NetworkBehaviour
{
    public static PoolManager Instance;

    public Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();
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
            Destroy(this);
        }
        pools = new Dictionary<GameObject, List<GameObject>>();
    }

    /// <summary>
    ///     For items that are network synced only!
    /// </summary>
    [Server]
    public GameObject PoolNetworkInstantiate(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        GameObject tempObject = null;
        bool makeNew = false;
        if (pools.ContainsKey(prefab))
        {
            if (pools[prefab].Count > 0)
            {
                //pool exists and has unused instances
                int index = pools[prefab].Count - 1;
                tempObject = pools[prefab][index];
                pools[prefab].RemoveAt(index);
                tempObject.GetComponent<ObjectBehaviour>().visibleState = true;
                tempObject.transform.position = position;
                tempObject.transform.rotation = rotation;
                tempObject.transform.localScale = prefab.transform.localScale;
                return tempObject;
            }
            //pool exists but is empty
            makeNew = true;
        }
        else
        {
            //pool for this prefab does not yet exist
            pools.Add(prefab, new List<GameObject>());
            makeNew = true;
        }
        if (makeNew)
        {
            tempObject = Instantiate(prefab, position, rotation);
            tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
            NetworkServer.Spawn(tempObject);
            return tempObject;
        }
        return tempObject;
    }

    /// <summary>
    ///     For non network stuff only! (e.g. bullets)
    /// </summary>
    public GameObject PoolClientInstantiate(GameObject prefab, Vector2 position, Quaternion rotation)
    {
        GameObject tempObject = null;
        bool makeNew = false;
        if (pools.ContainsKey(prefab))
        {
            if (pools[prefab].Count > 0)
            {
                //pool exists and has unused instances
                int index = pools[prefab].Count - 1;
                tempObject = pools[prefab][index];
                pools[prefab].RemoveAt(index);
                tempObject.SetActive(true);
                tempObject.transform.position = position;
                tempObject.transform.rotation = rotation;
                tempObject.transform.localScale = prefab.transform.localScale;
                return tempObject;
            }
            //pool exists but is empty
            makeNew = true;
        }
        else
        {
            //pool for this prefab does not yet exist
            pools.Add(prefab, new List<GameObject>());
            makeNew = true;
        }
        if (makeNew)
        {
            tempObject = Instantiate(prefab, position, rotation);
            tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
            return tempObject;
        }
        return tempObject;
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
        GameObject prefab = target.GetComponent<PoolPrefabTracker>().myPrefab;
        prefab.transform.position = Vector2.zero;
        target.GetComponent<ObjectBehaviour>().visibleState = false;
        pools[prefab].Add(target);
    }

    /// <summary>
    ///     For non network stuff only! (e.g. bullets)
    /// </summary>
    public void PoolClientDestroy(GameObject target)
    {
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
    public GameObject GetInstanceInactive(GameObject prefab)
    {
        GameObject tempObject = null;
        bool makeNew = false;
        if (pools.ContainsKey(prefab))
        {
            if (pools[prefab].Count > 0)
            {
                int index = pools[prefab].Count - 1;
                tempObject = pools[prefab][index];
                return tempObject;
            }
            makeNew = true;
        }
        else
        {
            pools.Add(prefab, new List<GameObject>());
            makeNew = true;
        }
        if (makeNew)
        {
            tempObject = Instantiate(prefab);
            tempObject.SetActive(false);
            tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;
            return tempObject;
        }
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