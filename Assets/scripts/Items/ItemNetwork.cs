using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;
using Network;

namespace Items {
    [RequireComponent(typeof(PhotonView))]
    public class ItemNetwork: Photon.PunBehaviour, IPunObservable {
        private bool synced = false;
		private EditModeControl snapControl;

        public void OnAddToInventory(string slotName) {
            this.photonView.RequestOwnership();
        }

        void Start() {
			snapControl = GetComponent<EditModeControl>();
            if(PhotonNetwork.connectedAndReady) {
                //Has been instantiated at runtime and you received instantiate of this object from photon on room join
                StartSync();
            }
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
            if(stream.isWriting) {
                stream.SendNext(transform.position);
            } else {
                // Network player, receive data
                this.transform.position = (Vector3) stream.ReceiveNext();
            }
        }

		//receive broadcast message when item is dropped from hand
		public void OnRemoveFromInventory()
		{
			if (snapControl != null) {
				snapControl.Snap();
			}
		}

        public override void OnJoinedRoom() {
            StartSync();
        }

        void StartSync() {
            NetworkItemDB.AddItem(photonView.viewID, gameObject);
            synced = true;
        }

    }
}