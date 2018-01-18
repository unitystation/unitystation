using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Equipment
{
	///Per-player equipment pool. Low-level item operations are here (though the lowest ones are in ObjectPool)
	///For items that are the ownership of players, the items are kept in a pool serverside and sprites references
	///sent to the client UI and playerobj.
	public class EquipmentPool : MonoBehaviour
	{
		private static EquipmentPool equipmentPool;
		private readonly Dictionary<NetworkInstanceId, ObjectPool> equipPools = new Dictionary<NetworkInstanceId, ObjectPool>();

		private GameObject objectPoolPrefab;

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

		private void Init()
		{
			Instance.transform.position = Vector2.zero;
			Instance.objectPoolPrefab = Resources.Load("ObjectPool") as GameObject;
		}

		public static void AddGameObject(GameObject player, GameObject gObj)
		{
			string playerName = player.name;
			NetworkInstanceId ownerId = player.GetComponent<NetworkIdentity>().netId;
			if (Instance.equipPools.ContainsKey(ownerId))
			{
				//add obj to pool
				Instance.equipPools[ownerId].AddGameObject(gObj);

				gObj.BroadcastMessage("OnAddToPool", ownerId, SendMessageOptions.DontRequireReceiver);
			}
			else
			{
				//set up new pool and then add the obj
				GameObject newPool =
					Instantiate(Instance.objectPoolPrefab, Vector2.zero, Quaternion.identity);
				newPool.transform.parent = Instance.transform;
				newPool.name = $"{playerName} ({ownerId})";
				Instance.equipPools.Add(ownerId, newPool.GetComponent<ObjectPool>());
				Instance.equipPools[ownerId].AddGameObject(gObj);
			}
//			Debug.LogFormat($"Added {gObj.name}({gObj.GetComponent<ItemAttributes>().itemName}) " +
//			                $"to {playerName}'s pool.size={Instance.equipPools[ownerId].currentObjects.Count}");
		}

		/// Disposing of objects that aren't supposed to be dropped on the ground
		public static void DisposeOfObject(GameObject player, GameObject gObj)
		{
			NetworkInstanceId ownerId = player.GetComponent<NetworkIdentity>().netId;
			if (!Instance.equipPools.ContainsKey(ownerId))
			{
				return;
			}
			Instance.equipPools[ownerId].DestroyGameObject(gObj);
			gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);
			//			Debug.LogFormat("{0}: destroyed {1}({2}) from pool. size={3} ", 
			//				playerName, gObj.name, gObj.GetComponent<ItemAttributes>().itemName, 
			//				Instance.equipPools[playerName].currentObjects.Count);
		}

		///When dropping items etc, remove them from the player equipment pool and place in scene
		public static void DropGameObject(GameObject player, GameObject gObj)
		{
			DropGameObject(player, gObj, player.transform.position);
		}

		///When dropping items etc, remove them from the player equipment pool and place in scene
		public static void DropGameObjectAtPos(GameObject player, GameObject gObj, Vector3 dropAtWorldPos)
		{
			DropGameObject(player, gObj, dropAtWorldPos);
		}

		//When placing items at a position etc also removes them from the player equipment pool and places it in scene
		public static void DropGameObject(GameObject player, GameObject gObj, Vector3 pos)
		{
			NetworkIdentity networkIdentity = player.GetComponent<NetworkIdentity>();
			if ( !networkIdentity )
			{
				Debug.LogWarning("Unable to drop as NetIdentity is gone");
				return;
			}
			NetworkInstanceId ownerId = networkIdentity.netId;
			if (!Instance.equipPools.ContainsKey(ownerId))
			{
				return;
			}
			Instance.equipPools[ownerId].DropGameObject(gObj, pos);
			gObj.BroadcastMessage("OnRemoveFromPool", null, SendMessageOptions.DontRequireReceiver);
			//			Debug.LogFormat("{0}: removed {1}({2}) from pool. size={3} ", 
			//				playerName, gObj.name, gObj.GetComponent<ItemAttributes>().itemName, 
			//				Instance.equipPools[playerName].currentObjects.Count);
		}

		public static void ClearPool(GameObject player)
		{
			NetworkInstanceId ownerId = player.GetComponent<NetworkIdentity>().netId;
			if (Instance.equipPools.ContainsKey(ownerId))
			{
				Instance.equipPools[ownerId].currentObjects.Clear();
			}
		}
	}
}