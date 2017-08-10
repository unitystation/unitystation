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
                if ( originator )
                {   //someone else tried to pick up
//                    originator.BroadcastMessage("TryToPickUpObject", gameObject);
                    originator.GetComponent<PlayerNetworkActions>().TryToPickUpObject(gameObject);
                }
                else
                {  //serverplayer picks something up himself
                   PlayerManager.LocalPlayerScript.playerNetworkActions.TryToPickUpObject(gameObject);  
                }
            }
        }
    }
}
