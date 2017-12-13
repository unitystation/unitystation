using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Equipment
{
    //For items that are the ownership of players, the items are kept in a pool serverside and sprites references
    //sent to the client UI and playerobj
    public class EquipmentPool : MonoBehaviour
    {
        private static EquipmentPool equipmentPool;

        public static EquipmentPool Instance
        {
            get
            {
                if (!equipmentPool)
                {
                    equipmentPool = FindObjectOfType<EquipmentPool>();
                    equipmentPool.Init();
                }
                return equipmentPool;
            }
        }

        private GameObject objectPoolPrefab;
        private Dictionary<string, ObjectPool> equipPools = new Dictionary<string, ObjectPool>();

        void Init()
        {
            Instance.transform.position = Vector2.zero;
            Instance.objectPoolPrefab = Resources.Load("ObjectPool") as GameObject;
        }

        public static void AddGameObject(GameObject player, GameObject gObj)
        {
            var playerName = player.name;
            if (Instance.equipPools.ContainsKey(playerName))
            {
				//add obj to pool
				Instance.equipPools[playerName].AddGameObject(gObj);

                var ownerId = player.GetComponent<NetworkIdentity>().netId;
                gObj.BroadcastMessage("OnAddToPool", ownerId, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
				//set up new pool and then add the obj
				GameObject newPool = Instantiate(Instance.objectPoolPrefab, Vector2.zero, Quaternion.identity) as GameObject;
                newPool.transform.parent = Instance.transform;
                newPool.name = playerName;
                Instance.equipPools.Add(playerName, newPool.GetComponent<ObjectPool>());
                Instance.equipPools[playerName].AddGameObject(gObj);
            }

            //			Debug.LogFormat("Added {1}({2}) to {0}'s pool.size={3}",
            //			playerName, gObj.name, gObj.GetComponent<ItemAttributes>().itemName, 
            //			Instance.equipPools[playerName].currentObjects.Count);
        }

        //ghetto W.E.T. method intended for disposing of objects that aren't supposed to be dropped on the ground 
        public static void DisposeOfObject(GameObject player, GameObject gObj)
        {
            var playerName = player.name;
            if (!Instance.equipPools.ContainsKey(playerName)) return;
            Instance.equipPools[playerName].DestroyGameObject(gObj);
            gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);
            //			Debug.LogFormat("{0}: destroyed {1}({2}) from pool. size={3} ", 
            //				playerName, gObj.name, gObj.GetComponent<ItemAttributes>().itemName, 
            //				Instance.equipPools[playerName].currentObjects.Count);
        }

        //When dropping items etc, remove them from the player equipment pool and place in scene
        public static void DropGameObject(GameObject player, GameObject gObj)
        {
            DropGameObject(player, gObj, PlayerList.Instance.connectedPlayers[player.name].transform.position);
        }

        //When placing items at a position etc also removes them from the player equipment pool and places it in scene
        public static void DropGameObject(GameObject player, GameObject gObj, Vector3 pos)
        {
            var playerName = player.name;
			if (!Instance.equipPools.ContainsKey(playerName)) {
				return;
			}
            Instance.equipPools[playerName].DropGameObject(gObj, pos);
            gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);
            //			Debug.LogFormat("{0}: removed {1}({2}) from pool. size={3} ", 
            //				playerName, gObj.name, gObj.GetComponent<ItemAttributes>().itemName, 
            //				Instance.equipPools[playerName].currentObjects.Count);
        }

        public static void ClearPool(string playerName)
        {
            if (Instance.equipPools.ContainsKey(playerName))
            {
                Instance.equipPools[playerName].currentObjects.Clear();
            }
        }
    }
}
