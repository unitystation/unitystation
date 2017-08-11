using System;
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
		protected string hand;

		public InputTrigger From(GameObject originator)
		{
			this.originator = originator;
			return this;
		}

		//really don't like this
		public InputTrigger With(string withWhat)
		{
			this.hand = withWhat;
			return this;
		}

		[Obsolete]
		public abstract void Interact();
	}
}