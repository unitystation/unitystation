using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Game {
    public class GameMatrix: MonoBehaviour {

        public static GameMatrix control;

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
        public void AddCupboard(int viewID, Cupboards.DoorTrigger theCupB) //To get transform.position then look at the DoorTrigger.transform.parent
        {
            cupboards.Add(viewID, theCupB);
        }

        public void RemoveItem(int viewID) { //For removing items from the game, on all clients
            photonView.RPC("MasterClientDestroyItem", PhotonTargets.MasterClient, viewID); //Get masterclient to remove item from game   
        }

        public void InstantiateItem(string prefabName, Vector3 pos, Quaternion rot, int itemGroup, object[] data) //Need to send this to the client as only the client can make scene objs
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
            items.Remove(viewID);
            photonView.RPC("RemoveItemOnNetwork", PhotonTargets.Others, viewID); //This removes the dictionary record on all clients including this
        }

        [PunRPC]
        void RemoveItemOnNetwork(int viewID) {
            items.Remove(viewID);
        }

    }
}