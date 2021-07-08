using System.Collections.Generic;
using UnityEngine;
namespace Doors.Modules
{
	public abstract class DoorModuleBase : MonoBehaviour
	{
		//Master Controller, assigned when the object spawns in.
		protected DoorMasterController master;

		protected virtual void Awake()
		{
			master = GetComponentInParent<DoorMasterController>();
		}

		public abstract ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States);

		public abstract ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States);

		public abstract ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States);

		/// <summary>
		/// Whether or not the door can opened or closed. This should only return false if the door is physically prevented
		/// from changing states, such as when welded shut or when the bolts are down.
		/// </summary>
		/// <returns>is the door free to change its state?</returns>
		public abstract bool CanDoorStateChange();
	}
}