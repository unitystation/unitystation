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

//		protected GameObject originator;
//		protected string hand;
//		public InputTrigger From(GameObject originator)
//		{
//			this.originator = originator;
//			return this;
//		}
//		//really don't like this
//		public InputTrigger With(string withWhat)
//		{
//			this.hand = withWhat;
//			return this;
//		}

		public void Interact()
		{
			Interact(PlayerManager.LocalPlayerScript.gameObject, UIManager.Hands.CurrentSlot.eventName);
		}

		public abstract void Interact(GameObject originator, string hand);
	}
}