using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Network {
	public class CabinetSync: NetworkBehaviour {

        private CabinetTrigger cabinetTrigger;
		private NetworkIdentity networkIdentity;

        private bool synced = false;

        void Start() {
            cabinetTrigger = GetComponent<CabinetTrigger>();
			networkIdentity = GetComponent<NetworkIdentity>();
                StartSync();
        }

        [Command]
		void CmdSendCurrentState(NetworkInstanceId playerRequesting) //Master client must update all other clients on join on shutter state
        {
			RpcReceiveCurrentState(playerRequesting, cabinetTrigger.IsClosed);
		}

        [ClientRpc]
		void RpcReceiveCurrentState(NetworkInstanceId playerIdent, bool closedState) {
			if(networkIdentity.netId == playerIdent) {
				cabinetTrigger.SetState(closedState, playerIdent, false);
            }
        }

		void OnConnectedToServer() {
            //Update on join if this item was not instantiated by the game and is apart of the map
            StartSync();
        }

        void StartSync() {
            if(!synced) {
				if(!isServer) {
					CmdSendCurrentState(networkIdentity.netId);
				}

                synced = true;
            }
        }
    }
}