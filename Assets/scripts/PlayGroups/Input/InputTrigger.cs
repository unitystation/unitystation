using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace InputControl {

	public abstract class InputTrigger: NetworkBehaviour {

		public void Trigger() {
			Interact();
		}

		public abstract void Interact();
	}
}