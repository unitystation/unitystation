using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;
using Network;

namespace Items {
    [RequireComponent(typeof(PhotonView))]
    public class ItemNetwork: Photon.PunBehaviour, IPunObservable {

        public void OnAddToInventory(string slotName){
            this.photonView.RequestOwnership();
            Debug.Log("Request ownership");
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
            if(stream.isWriting) {
                
                stream.SendNext(transform.position);
              
            } else {
                // Network player, receive data
                this.transform.position = (Vector3) stream.ReceiveNext();
            }
        }
  
    }
}