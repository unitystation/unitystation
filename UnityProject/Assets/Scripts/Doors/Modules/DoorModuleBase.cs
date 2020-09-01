using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Doors
{
	[RequireComponent(typeof(DoorMasterController))]
	public abstract class DoorModuleBase : NetworkBehaviour
	{
		//Master Controller, assigned when the object spawns in.
		private DoorMasterController master;

		private void Awake()
		{
			master = GetComponent<DoorMasterController>();
		}

		public abstract ModuleSignal OpenInteraction(HandApply interaction);

		public abstract ModuleSignal ClosedInteraction(HandApply interaction);
	}
}