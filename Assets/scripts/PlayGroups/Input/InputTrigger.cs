using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace InputControl {

	public abstract class InputTrigger: NetworkBehaviour {

        public void Trigger() {
            if(PlayerManager.LocalPlayerScript.IsInReach(transform)) {
                Interact();
            }
        }


        public abstract void Interact();
    }
}