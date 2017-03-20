using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Network {
    public class NetworkItemDB: MonoBehaviour {
        // Current uses:
        // this instance is used to add items to cupboards across all clients,
        // using their photon id's to determine which item should be the child of which cupboard.
        // each item while add itself to the Dictionary when it is instantiated if connected to photon.

        //items
        public Dictionary<int, GameObject> items = new Dictionary<int, GameObject>();
        //cupboards
        public Dictionary<int, Cupboards.ClosetControl> cupboards = new Dictionary<int, Cupboards.ClosetControl>();

        private PhotonView photonView;
        //This is to sync destroy and instantiate calls with masterclient

        private static NetworkItemDB networkItemDB;

        public static NetworkItemDB Instance {
            get {
                if(!networkItemDB) {
                    networkItemDB = FindObjectOfType<NetworkItemDB>();
                    networkItemDB.Init();
                }

                return networkItemDB;
            }
        }

        private void Init() {
            photonView = gameObject.GetComponent<PhotonView>();
        }

        public static Dictionary<int, GameObject> Items {
            get {
                return Instance.items;
            }
        }

        public static Dictionary<int, Cupboards.ClosetControl> Cupboards {
            get {
                return Instance.cupboards;
            }
        }

        //Add each item to the items dictionary along with their photonView.viewID as key
        public static void AddItem(int viewID, GameObject theItem) {
            if(!Items.ContainsKey(viewID)) {
                Items.Add(viewID, theItem);
            } else {
                Debug.Log("Warning! item already exists in dictionary. ViewID: " + viewID + " Item " + theItem.name);
            }
        }

        //Add each cupB to the items dictionary along with its photonView.viewID as key (this will be doortriggers)
        public static void AddCupboard(int viewID, Cupboards.ClosetControl theCupB) 
        {
            Cupboards.Add(viewID, theCupB);

            //Note: To get transform.position then look at the DoorTrigger.transform.parent
        }

        //For removing items from the game, on all clients
        public static void RemoveItem(int viewID) {
            Instance.photonView.RPC("MasterClientDestroyItem", PhotonTargets.MasterClient, viewID);   
        }

        //Need to send this to the client as only the client can make scene objs
        public static void InstantiateItem(string prefabName, Vector3 pos, Quaternion rot, int itemGroup, object[] data) 
        {
            Instance.photonView.RPC("MasterClientCreateItem", PhotonTargets.MasterClient, prefabName, pos, rot, itemGroup, data);
        }

        [PunRPC] //You can call this directly if you are the master client
        public GameObject MasterClientCreateItem(string prefabName, Vector3 pos) {
            return PhotonNetwork.InstantiateSceneObject(prefabName, pos, Quaternion.identity, 0, null);
        }

        [PunRPC] //You can call this directly if you are the master client
        public GameObject MasterClientCreateItem(string prefabName, Vector3 pos, Quaternion rot, int itemGroup, object[] data) {
            return PhotonNetwork.InstantiateSceneObject(prefabName, pos, rot, itemGroup, data);
        }

        [PunRPC]
        void MasterClientDestroyItem(int viewID) {
            //Only objects can be destroyed by client
            PhotonNetwork.Destroy(items[viewID]);
            //This removes the dictionary record on all clients including this
            photonView.RPC("RemoveItemOnNetwork", PhotonTargets.All, viewID); 
        }

        [PunRPC]
        void RemoveItemOnNetwork(int viewID) {
            items.Remove(viewID);
        }

    }
}