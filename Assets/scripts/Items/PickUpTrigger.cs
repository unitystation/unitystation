using UnityEngine;
using System.Collections;
using PlayGroup;
using InputControl;
using System;

namespace Items {
    public class PickUpTrigger: InputTrigger {
        public override void Interact() {
            if ( !isServer )
            {
                InteractMessage.Send(gameObject);
            }
            else
            {
                // try to add the item to hand
                PlayerManager.LocalPlayerScript.playerNetworkActions.TryToPickUpObject(gameObject);
            }
        }
    }
}
