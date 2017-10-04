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
                var uiSlotObject = new UISlotObject(hand, gameObject);

                //PreCheck
                if ( ClientApprove(uiSlotObject) )
                {
                    //Simulation
                    UIManager.UpdateSlot(uiSlotObject);

                    //Client informs server of interaction attempt
                    InteractMessage.Send(gameObject, hand);
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

        private bool ClientApprove(UISlotObject uiSlotObject)
        {
            return UIManager.CanPutItemToSlot(uiSlotObject);
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

            return ps.playerNetworkActions.AddItem(gameObject, slotName, false, false);
        }
    }
}
