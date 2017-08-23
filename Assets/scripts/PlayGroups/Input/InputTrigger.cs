using System;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;

namespace InputControl {

	public abstract class InputTrigger: NetworkBehaviour {

		public void Trigger() {
			Interact();
		}

		private void Interact()
		{
			Interact(PlayerManager.LocalPlayerScript.gameObject, UIManager.Hands.CurrentSlot.eventName);
		}

		public abstract void Interact(GameObject originator, string hand);
		
		//TODO: move more common stuff here?
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