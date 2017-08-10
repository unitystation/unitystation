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

		protected GameObject originator;

		public InputTrigger From(GameObject originator)
		{
			this.originator = originator;
			return this;
		}

		public abstract void Interact();
	}
}