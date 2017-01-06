using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Network {

    public class ShutterSync: Photon.PunBehaviour {
        private SwitchShutters switchShutters;

        private bool synced = false;

        void Start() {
            switchShutters = GetComponent<SwitchShutters>();

            if(PhotonNetwork.connectedAndReady) {
                //Has been instantiated at runtime and you received instantiate of this object from photon on room join
                StartSync();
            }
        }
        
        [PunRPC]
        void SendCurrentState() //Master client must update all other clients on join on shutter state
        {
            photonView.RPC("ReceiveCurrentState", PhotonTargets.Others, switchShutters.IsClosed);
        }

        [PunRPC]
        void ReceiveCurrentState(bool closedState) {
            if(closedState) {
                switchShutters.CloseShutters();
            } else {
                switchShutters.OpenShutters();
            }
        }

        public override void OnJoinedRoom() {
            //Update on join if this item was not instantiated by the game and is apart of the map
            StartSync();
        }

        void StartSync() {
            if(!synced) {
                if(!PhotonNetwork.isMasterClient) {
                    photonView.RPC("SendCurrentState", PhotonTargets.MasterClient, null);
                }

                synced = true;
            }
        }
    }
}