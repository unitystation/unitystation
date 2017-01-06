using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;
using Network;

namespace Items {
    [RequireComponent(typeof(PhotonView))]
    public class ItemNetwork: Photon.PunBehaviour {

        private Vector3 lastPos;
        //Catch the last pos of the transform at the end of the frame
        private bool synced = false;

        void Start() {
            lastPos = transform.position;
            if(PhotonNetwork.connectedAndReady) {
                //Has been instantiated at runtime and you received instantiate from photon on room join
                StartSync();
            }
        }

        void Update() {
            if(photonView != null) {
                if(transform.position != lastPos && PhotonNetwork.connectedAndReady) { //if the item has been moved by someone then update its transform to all other clients
                    photonView.RPC("UpdateItemTransform", PhotonTargets.All, new object[] { transform.position });
                }
            }
            lastPos = transform.position;
        }

        [PunRPC]
        void UpdateItemTransform(Vector3 pos) {
            lastPos = pos;
            transform.position = pos;
        }

        //PUN Sync
        [PunRPC]
        void SendCurrentState() {
            if(PhotonNetwork.isMasterClient) {
                photonView.RPC("UpdateItemTransform", PhotonTargets.Others, transform.position);
            }
        }

        //PUN Callbacks

        public override void OnJoinedRoom() {
            //Update on join if this item was not instantiated by the game and is apart of the map
            StartSync();

        }

        void StartSync() {
            if(!synced) {
                if(!PhotonNetwork.isMasterClient) {
                    //If you are not the master then update the current IG state of this object from the master
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, null);
                }

                NetworkItemDB.AddItem(photonView.viewID, this.gameObject);
                synced = true;
            }
        }
    }
}