using System.Collections.Generic;
using UnityEngine;
namespace Doors.Modules
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
		public virtual void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return;
		}

		//Interactions when the door is closed
		public virtual void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return;
		}

		public virtual void BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return;
		}
	}
}