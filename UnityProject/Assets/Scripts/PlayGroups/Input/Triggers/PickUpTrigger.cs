using InputControl;
using PlayGroup;
using PlayGroups.Input;
using Tilemaps.Scripts.Behaviours.Objects;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Items
{
    public class PickUpTrigger : InputTrigger
    {
        private void Start()
        {
            CheckSpriteOrder();
        }
        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            if (originator.GetComponent<PlayerScript>().canNotInteract())
                return;

            if (!isServer)
            {
                var uiSlotObject = new UISlotObject(hand, gameObject);

                //PreCheck
                if (UIManager.CanPutItemToSlot(uiSlotObject))
                {
                    //Simulation
                    //                    UIManager.UpdateSlot(uiSlotObject);

                    //Client informs server of interaction attempt
                    InteractMessage.Send(gameObject, hand);
                }
            }
            else
            {    //Server actions
                if (ValidatePickUp(originator, hand))
                {
                    GetComponent<RegisterItem>().Unregister();
                }
                else
                {
                    //Rollback prediction
                    //                    originator.GetComponent<PlayerNetworkActions>().RollbackPrediction(hand);
                }
            }
        }

        [Server]
        public bool ValidatePickUp(GameObject originator, string handSlot = null)
        {
            var ps = originator.GetComponent<PlayerScript>();
            var slotName = handSlot ?? UIManager.Hands.CurrentSlot.eventName;
            if (PlayerManager.PlayerScript == null || !ps.playerNetworkActions.Inventory.ContainsKey(slotName))
            {
                return false;
            }

            //set ForceInform to false for simulation
            return ps.playerNetworkActions.AddItem(gameObject, slotName, false /*, false*/);
        }

        /// <summary>
        /// If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
        /// interacting with the object via the InputTrigger especially when placed on
        /// tables. This method makes sure that it is never 0 on start
        /// </summary>
        private void CheckSpriteOrder()
        {
            SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
            if (sR != null)
            {
                if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0)
                {
                    sR.sortingOrder = 1;
                }
            }
        }
    }
}
