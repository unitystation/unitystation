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
	}
}