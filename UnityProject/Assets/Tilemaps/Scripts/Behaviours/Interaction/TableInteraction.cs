using System;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Scripts.Behaviours.Interaction
{
    public class TableInteraction : TileInteraction
    {
        public TableInteraction(GameObject gameObject, GameObject originator, Vector3 position, string hand) : base(gameObject, originator, position, hand)
        {
        }

        public override void ClientAction()
        {
            var slot = UIManager.Hands.CurrentSlot;

            // Client pre-approval
            if (slot.CanPlaceItem())
            {
                InteractMessage.Send(gameObject, position, slot.eventName);
            }
        }

        public override void ServerAction()
        {
            var ps = originator.GetComponent<PlayerScript>();
            var item = ps.playerNetworkActions.Inventory[hand];
            if (ps.canNotInteract() || !ps.IsInReach(position) || item == null)
            {
                return;
            }

            var targetPosition = position;
            targetPosition.z = -0.2f;
            ps.playerNetworkActions.PlaceItem(hand, targetPosition, gameObject);

            item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
        }
    }
}