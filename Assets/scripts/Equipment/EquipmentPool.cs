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

		public static EquipmentPool Instance {
			get { 
				if (!equipmentPool) {
					equipmentPool = FindObjectOfType<EquipmentPool>();
					equipmentPool.Init();
				}
				return equipmentPool;
			}
		}

		private GameObject objectPoolPrefab;
		private Dictionary<string,ObjectPool> equipPools = new Dictionary<string, ObjectPool>();

		void Init()
		{
			Instance.transform.position = Vector2.zero;
			Instance.objectPoolPrefab = Resources.Load("ObjectPool")as GameObject;
		}

		public static void AddGameObject(GameObject player, GameObject gObj)
		{
			var playerName = player.name;
			if (Instance.equipPools.ContainsKey(playerName)) {
				//add obj to pool
				Instance.equipPools[playerName].AddGameObject(gObj);

				var ownerId = player.GetComponent<NetworkIdentity>().netId;
				gObj.BroadcastMessage("OnAddToPool", ownerId, SendMessageOptions.DontRequireReceiver);
			} else {
				//set up new pool and then add the obj
				GameObject newPool = Instantiate(Instance.objectPoolPrefab, Vector2.zero, Quaternion.identity) as GameObject;
				newPool.transform.parent = Instance.transform;
				newPool.name = playerName;
				Instance.equipPools.Add(playerName, newPool.GetComponent<ObjectPool>());
				Instance.equipPools[playerName].AddGameObject(gObj);
			}
		}

		//When dropping items etc, remove them from the player equipment pool and place in scene
		public static void DropGameObject(string playerName, GameObject gObj)
		{
			if (Instance.equipPools.ContainsKey(playerName)) {
				Instance.equipPools[playerName].DropGameObject(gObj, PlayerList.Instance.connectedPlayers[playerName].transform.position);
				gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);
			}
		}

		//When placing items at a position etc also removes them from the player equipment pool and places it in scene
		public static void DropGameObject(string playerName, GameObject gObj, Vector3 pos)
		{
			if (Instance.equipPools.ContainsKey(playerName)) {
				Instance.equipPools[playerName].DropGameObject(gObj, pos);
				gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);

			}
		}
	}
}
