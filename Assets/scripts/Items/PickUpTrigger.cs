using InputControl;
using PlayGroup;
using UI;
using UnityEngine;

namespace Items {
    public class PickUpTrigger: InputTrigger {
        public override void Interact(GameObject originator, string hand) {
            if ( !isServer )
            {
                InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
            }
            else
            {
                if ( originator )
                {   //someone else tried to pick up
//                    originator.BroadcastMessage("TryToPickUpObject", gameObject);
                    originator.GetComponent<PlayerNetworkActions>().
                        TryToPickUpObject(gameObject, hand);
                }
                else
                {  //serverplayer picks something up himself
                   PlayerManager.LocalPlayerScript.playerNetworkActions.
                       TryToPickUpObject(gameObject);  
                }
            }
        }
    }
}
