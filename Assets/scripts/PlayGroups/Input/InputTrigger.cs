using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace InputControl {

    public abstract class InputTrigger: Photon.PunBehaviour {

        public void Trigger() {

            if(PlayerManager.LocalPlayerScript.IsInReach(transform)) {
                Interact();
            }
        }


        public abstract void Interact();
    }
}