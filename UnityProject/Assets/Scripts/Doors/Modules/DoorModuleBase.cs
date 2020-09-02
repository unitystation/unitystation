using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Doors
{
	public abstract class DoorModuleBase : MonoBehaviour
	{
		//Master Controller, assigned when the object spawns in.
		protected DoorMasterController master;

		protected virtual void Awake()
		{
			master = GetComponentInParent<DoorMasterController>();
		}

		public abstract ModuleSignal OpenInteraction(HandApply interaction);

		public abstract ModuleSignal ClosedInteraction(HandApply interaction);

		//Whether or not the door can opened or closed. This should only return false if the door is physically prevented
		//from changing states, such as when welded shut or when the bolts are down.
		public abstract bool CanDoorStateChange();
	}
}