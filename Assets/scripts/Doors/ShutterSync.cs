using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network {

    public class ShutterSync: Photon.PunBehaviour {
        private bool synced = false;

        private ShutterController shutterController;

        void Start() {
            shutterController = GetComponent<ShutterController>();

            if(PhotonNetwork.connectedAndReady) {
                StartSync();
            }
        }

        [PunRPC]
        void SendCurrentState(string playerRequesting) {
			if (shutterController != null) {
				photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, playerRequesting, shutterController.IsClosed);
			}
        }

        [PunRPC]
        void ReceiveCurrentState(string playerRequesting, bool isClosed) {
            if(PhotonNetwork.player.NickName == playerRequesting) {
                shutterController.SyncState(isClosed);
            }
        }

        public override void OnJoinedRoom() {
            StartSync();
        }

        void StartSync() {
            if(!synced) {
                photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, PhotonNetwork.player.NickName);

                synced = true;
            }
        }
    }
}