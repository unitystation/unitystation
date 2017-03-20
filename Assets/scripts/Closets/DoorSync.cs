using Cupboards;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network {

    public class DoorSync: Photon.PunBehaviour {
        private bool synced = false;

        private ClosetControl closetControl;
        private LockLightController lockLight;
        private GameObject items;

        void Start() {
            closetControl = GetComponent<ClosetControl>();
            lockLight = transform.GetComponentInChildren<LockLightController>();
            items = transform.FindChild("Items").gameObject;

            if(PhotonNetwork.connectedAndReady) {
                //Has been instantiated at runtime and you received instantiate of this object from photon on room join
                StartSync();
            }
        }

        //PUN Sync
        [PunRPC]
        void SendCurrentState(string playerRequesting) {
            if(PhotonNetwork.isMasterClient) {

                PhotonView[] objectsInCupB = items.GetPhotonViewsInChildren();
                if(objectsInCupB != null) {
                    foreach(PhotonView p in objectsInCupB) {
                        photonView.RPC("DropItem", PhotonTargets.Others, p.viewID); //Make sure all spawneditems are where they should be on new player join
                    }
                }

                if(lockLight != null) {
                    photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, lockLight.IsLocked(), closetControl.IsClosed, transform.parent.transform.position); //Gather the values and send back
                } else {
                    photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, false, closetControl.IsClosed, transform.parent.transform.position); //Gather the values and send back 
                }
            }
        }

        [PunRPC]
        void ReceiveCurrentState(string playerIdent, bool isLocked, bool isClosed, Vector3 pos) {
            if(PhotonNetwork.player.NickName == playerIdent) {

                if(isClosed) {
                    closetControl.Close();
                } else {
                    closetControl.Open();
                    Debug.Log("open door");
                }

                if(lockLight != null) {
                    if(isLocked) {
                        lockLight.Lock();
                    } else {
                        lockLight.Unlock();
                    }
                }

                if(transform.parent.transform.position != pos) //Position of cupboard
                {
                    transform.parent.transform.position = pos;
                }
            }
        }

        public override void OnJoinedRoom() {
            StartSync();
        }

        void StartSync() {
            if(!synced) {
                if(!PhotonNetwork.isMasterClient) {
                    //If you are not the master then update the current IG state of this object from the master
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, PhotonNetwork.player.NickName);
                }

                NetworkItemDB.AddCupboard(photonView.viewID, closetControl);
                synced = true;
            }
        }
    }
}