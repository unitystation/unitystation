using InputControl;
using PlayGroup;
using Tilemaps.Scripts.Behaviours.Layers;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Scripts.Behaviours
{
    public class TileTrigger : InputTrigger
    {
        private Layer layer;
        
        private void Start()
        {
            layer = GetComponent<Layer>();
        }

        public override void Interact(GameObject originator, Vector3 position, string hand)
        {
            if (!isServer)
            {
                var slot = UIManager.Hands.CurrentSlot;

                // Client pre-approval
                if (slot.CanPlaceItem())
                {
                    InteractMessage.Send(gameObject, position, slot.eventName);
                }
            }
            else
            {   //Server actions
                ValidateTableInteraction(originator, position, hand);
            }
        }

        [Server]
        private bool ValidateTableInteraction(GameObject originator, Vector3 position, string hand)
        {
            var ps = originator.GetComponent<PlayerScript>();
            if (ps.canNotInteract() || !ps.IsInReach(position))
            {
                return false;
            }

            GameObject item = ps.playerNetworkActions.Inventory[hand];
            if (item == null) return false;
            var targetPosition = position; //Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPosition.z = -0.2f;
            ps.playerNetworkActions.PlaceItem(hand, targetPosition, gameObject);
            item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

            return true;
        }
    }
}