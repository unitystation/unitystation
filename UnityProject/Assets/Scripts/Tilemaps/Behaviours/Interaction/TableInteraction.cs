using PlayGroup;
using UI;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Interaction
{
	public class TableInteraction : TileInteraction
	{
		public TableInteraction(GameObject gameObject, GameObject originator, Vector3 position, string hand) : base(
			gameObject, originator, position, hand)
		{
		}

		public override void ClientAction()
		{
			UI_ItemSlot slot = UIManager.Hands.CurrentSlot;

			// Client pre-approval
			if (slot.CanPlaceItem())
			{
				InteractMessage.Send(gameObject, position, slot.eventName);
			}
		}

		public override void ServerAction()
		{
			PlayerScript ps = originator.GetComponent<PlayerScript>();
			GameObject item = ps.playerNetworkActions.Inventory[hand];
			if (ps.canNotInteract() || !ps.IsInReach(position) || item == null)
			{
				return;
			}

			Vector3 targetPosition = position;
			targetPosition.z = -0.2f;
			ps.playerNetworkActions.CmdPlaceItem(hand, targetPosition, gameObject);

			item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
		}
	}
}