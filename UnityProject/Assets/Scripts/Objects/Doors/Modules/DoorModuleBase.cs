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


		public virtual ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public virtual ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public virtual ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		/// <summary>
		/// Whether or not the door can opened or closed. This should only return false if the door is physically prevented
		/// from changing states, such as when welded shut or when the bolts are down.
		/// </summary>
		/// <returns>is the door free to change its state?</returns>
		public virtual bool CanDoorStateChange()
		{
			return true;
		}
	}
}