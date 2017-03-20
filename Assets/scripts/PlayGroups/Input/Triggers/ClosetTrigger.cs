using Cupboards;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputControl {

    public class ClosetTrigger: InputTrigger {

        private ClosetControl closetControl;
        private LockLightController lockLight;

        void Start() {
            closetControl = GetComponent<ClosetControl>();
            lockLight = transform.GetComponentInChildren<LockLightController>();
        }

        public override void Interact() {
            if(lockLight != null && lockLight.IsLocked()) {
                photonView.RPC("LockLight", PhotonTargets.All);
            } else {
                if(closetControl.IsClosed) {
                    photonView.RPC("Open", PhotonTargets.All);
                } else if(!closetControl.TryDropItem()) {
                    photonView.RPC("Close", PhotonTargets.All);
                }
            }
        }
    }
}