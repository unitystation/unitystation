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
            {
                //Prediction
                if ( ClientApprove(hand) )
                {
                    //Client informs server of interaction attempt
                    InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
                }
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
                    originator.GetComponent<PlayerNetworkActions>().RollbackPrediction(hand);
                }
            }
        }

        private bool ClientApprove(string hand)
        {
            var uiSlotObject = new UISlotObject(hand, gameObject);
            if ( !UIManager.CanPlaceItem(uiSlotObject) )
            {
                return false;
            }
            UIManager.UpdateSlot(uiSlotObject);
            return true;
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
