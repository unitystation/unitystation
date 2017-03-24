using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


namespace Network {
	public class NetworkItemDB: NetworkBehaviour {
        // Current uses:
        // this instance is used to add items to cupboards across all clients,
        // using their photon id's to determine which item should be the child of which cupboard.
        // each item while add itself to the Dictionary when it is instantiated if connected to photon.

        //items
		public Dictionary<NetworkInstanceId, GameObject> items = new Dictionary<NetworkInstanceId, GameObject>();
        //cupboards
		public Dictionary<NetworkInstanceId, Cupboards.ClosetControl> cupboards = new Dictionary<NetworkInstanceId, Cupboards.ClosetControl>();

        private static NetworkItemDB networkItemDB;

        public static NetworkItemDB Instance {
            get {
                if(!networkItemDB) {
                    networkItemDB = FindObjectOfType<NetworkItemDB>();
                }

                return networkItemDB;
            }
        }

		public static Dictionary<NetworkInstanceId, GameObject> Items {
            get {
                return Instance.items;
            }
        }

		public static Dictionary<NetworkInstanceId, Cupboards.ClosetControl> Cupboards {
            get {
                return Instance.cupboards;
            }
        }

        //Add each item to the items dictionary along with their photonView.viewID as key
		public static void AddItem(NetworkInstanceId ID, GameObject theItem) {
            if(!Items.ContainsKey(ID)) {
                Items.Add(ID, theItem);
            } else {
                Debug.Log("Warning! item already exists in dictionary. Item " + theItem.name);
            }
        }

        //Add each cupB to the items dictionary along with its netID as key (this will be doortriggers)
		public static void AddCupboard(NetworkInstanceId ID, Cupboards.ClosetControl theCupB) 
        {
            Cupboards.Add(ID, theCupB);

            //Note: To get transform.position then look at the DoorTrigger.transform.parent
        }

        //For removing items from the game, on all clients
		public static void RemoveItem(NetworkInstanceId ID) { 
			Instance.CmdServerDestroyItem(ID);
        }

        //Need to send this to the client as only the client can make scene objs
		[Command]
        public void CmdInstantiateItem(GameObject prefab, Vector3 pos, Quaternion rot) 
        {
			GameObject obj = Instantiate(prefab, pos, rot);
//			NetworkServer.Spawn(obj);
        }

//		[Command]
//		public void CmdInstantiateGameObject(GameObject prefab, Vector3 pos, Quaternion rot, NetworkInstanceId requestingPlayer) 
//		{
//			GameObject obj = Instantiate(prefab, pos, rot);
//			NetworkServer.Spawn(obj);
//		}

        [Command]
		void CmdServerDestroyItem(NetworkInstanceId ID) {
            //Only objects can be destroyed by client
			NetworkServer.Destroy(items[ID]);
            //This removes the dictionary record on all clients
			RpcRemoveItemOnClients(ID);
        }

        [ClientRpc]
		void RpcRemoveItemOnClients(NetworkInstanceId ID) {
            items.Remove(ID);
        }

    }
}