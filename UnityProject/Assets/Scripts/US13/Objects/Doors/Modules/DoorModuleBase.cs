using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;

namespace US13.Objects.Doors.Modules
{
	public class DoorModuleBase : MonoBehaviour
	{
		//Master Controller, assigned when the object spawns in.
		protected DoorMasterController master;

		protected virtual void Awake()
		{
			master = GetComponentInParent<DoorMasterController>();
		}


		//Interactions when the doors open
		public virtual void OpenInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			return;
		}

		//Interactions when the door is closed
		public virtual void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
		{
			return;
		}

		public virtual void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{
			return;
		}
	}
}