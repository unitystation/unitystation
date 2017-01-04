using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Network {
    public class NetworkItemDB: MonoBehaviour {

        public static NetworkItemDB control;

        // Current uses:
        // this instance is used to add items to cupboards across all clients,
        // using their photon id's to determine which item should be the child of which cupboard.
        // each item while add itself to the Dictionary when it is instantiated if connected to photon.

        //items
        public Dictionary<int, GameObject> items = new Dictionary<int, GameObject>();
        //cupboards
        public Dictionary<int, Cupboards.DoorTrigger> cupboards = new Dictionary<int, Cupboards.DoorTrigger>();

        private PhotonView photonView;
        //This is to sync destroy and instantiate calls with masterclient

        void Awake() {

            if(control == null) {
                control = this;
            } else {
                Destroy(this);
            }

            photonView = gameObject.GetComponent<PhotonView>();
        }

        //Add each item to the items dictionary along with their photonView.viewID as key
        public void AddItem(int viewID, GameObject theItem) {
            if(!items.ContainsKey(viewID)) {
                items.Add(viewID, theItem);
            } else {
                Debug.Log("Warning! item already exists in dictionary. ViewID: " + viewID + " Item " + theItem.name);
            }
        }

        //Add each cupB to the items dictionary along with its photonView.viewID as key (this will be doortriggers)
        public void AddCupboard(int viewID, Cupboards.DoorTrigger theCupB) 
        {
            cupboards.Add(viewID, theCupB);

            //Note: To get transform.position then look at the DoorTrigger.transform.parent
        }

        //For removing items from the game, on all clients
        public void RemoveItem(int viewID) { 
            photonView.RPC("MasterClientDestroyItem", PhotonTargets.MasterClient, viewID);   
        }

        //Need to send this to the client as only the client can make scene objs
        public void InstantiateItem(string prefabName, Vector3 pos, Quaternion rot, int itemGroup, object[] data) 
        {
            photonView.RPC("MasterClientCreateItem", PhotonTargets.MasterClient, prefabName, pos, rot, itemGroup, data);
        }

        [PunRPC] //You can call this directly if you are the master client
        public void MasterClientCreateItem(string prefabName, Vector3 pos, Quaternion rot, int itemGroup, object[] data) {
            PhotonNetwork.InstantiateSceneObject(prefabName, pos, rot, itemGroup, data);
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