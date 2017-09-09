using InputControl;
using Matrix;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Items {
    public class PickUpTrigger: InputTrigger {
        public override void Interact(GameObject originator, string hand) {
            if ( !isServer )
            {    //Client informs server of interaction attempt
                InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
            }
            else
            {    //Server actions
                if (!ValidatePickUp(originator, hand))
                {
                    //Rollback prediction
                }
                else
                {
                    GetComponent<RegisterTile>().RemoveTile();
                }
            }
        }
    }
}
