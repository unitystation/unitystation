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
	}
}