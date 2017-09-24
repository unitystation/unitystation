using InputControl;
using Matrix;
using PlayGroup;
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
		public override void Interact(GameObject originator, string hand)
		{
			if (!isServer) {    //Client informs server of interaction attempt
				InteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
			} else {    //Server actions
				if (ValidatePickUp(originator, hand)) {
					GetComponent<RegisterTile>().RemoveTile();
				}
			}
		}

		[Server]
		public bool ValidatePickUp(GameObject originator, string handSlot = null)
		{
			var ps = originator.GetComponent<PlayerScript>();
			var slotName = handSlot ?? UIManager.Hands.CurrentSlot.eventName;
			if (PlayerManager.PlayerScript == null || !ps.playerNetworkActions.Inventory.ContainsKey(slotName)) {
				return false;
			}

			return ps.playerNetworkActions.AddItem(gameObject, slotName);
		}

		/// <summary>
		/// If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
		/// interacting with the object via the InputTrigger especially when placed on
		/// tables. This method makes sure that it is never 0 on start
		/// </summary>
		private void CheckSpriteOrder()
		{
			SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
			if (sR != null) {
				if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0) {
					sR.sortingOrder = 1;
				}
			}
		}
	}
}
