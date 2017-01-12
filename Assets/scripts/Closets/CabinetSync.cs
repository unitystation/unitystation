using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network {
    public class CabinetSync: Photon.PunBehaviour {

        private CabinetTrigger cabinetTrigger;

        private bool synced = false;

        void Start() {
            cabinetTrigger = GetComponent<CabinetTrigger>();
            if (PhotonNetwork.connectedAndReady)
            {
                StartSync();
            }
        }

        [PunRPC]
        void SendCurrentState(string playerRequesting) //Master client must update all other clients on join on shutter state
        {
            photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, cabinetTrigger.IsClosed, cabinetTrigger.ItemViewID);
        }

        [PunRPC]
        void ReceiveCurrentState(string playerIdent, bool closedState, int itemViewID) {
            if(PhotonNetwork.player.NickName == playerIdent) {
                cabinetTrigger.SyncState(closedState, itemViewID, false);
            }
        }

        public override void OnJoinedRoom() {
            //Update on join if this item was not instantiated by the game and is apart of the map
            StartSync();
        }

        void StartSync() {
            if(!synced) {
                if(!PhotonNetwork.isMasterClient) {
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, PhotonNetwork.player.NickName);
                }

                synced = true;
            }
        }
    }
}