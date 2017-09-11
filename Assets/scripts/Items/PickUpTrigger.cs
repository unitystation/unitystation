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
            {   //Client informs server of interaction attempt
                InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
                //Prediction
                UIManager.UpdateSlot(new UISlotObject(hand, gameObject));
            }
            else
            {    //Server actions
                if (ValidatePickUp(originator, hand))
                {
                    GetComponent<RegisterTile>().RemoveTile();
                }
                else
                {
                    //Rollback prediction
                    //todo think of caching?
                    var pna = originator.GetComponent<PlayerNetworkActions>();
                    UpdateSlotMessage.Send(originator, hand, pna.Inventory[hand]);
                }
            }
        }
        
        [Server]
        public bool ValidatePickUp(GameObject originator, string handSlot = null)
        {
            var ps = originator.GetComponent<PlayerScript>();
            var slotName = handSlot ?? UIManager.Hands.CurrentSlot.eventName;
            if ( PlayerManager.PlayerScript == null || !ps.playerNetworkActions.Inventory.ContainsKey(slotName) )
            {
                return false;
            }

            return ps.playerNetworkActions.AddItem(gameObject, slotName);
        }
    }
}
