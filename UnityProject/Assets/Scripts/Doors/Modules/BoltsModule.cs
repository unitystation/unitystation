using NaughtyAttributes;
using UnityEngine;

namespace Doors.Modules
{
	public class BoltsModule : DoorModuleBase
	{
		private bool boltsDown = false;
		private bool boltsLights = true;

		/// <summary>
		/// Set the current state for this door's bolts.
		/// </summary>
		/// <param name="state">True means the bolts are down and the door can't be opened</param>
		[ContextMenu("Set bolt state")]
		public void SetBoltsState(bool state)
		{
			boltsDown = state;

			if (boltsDown && boltsLights && master.HasPower)
			{
				master.DoorAnimator.TurnOnBoltsLight();
			}
			else
			{
				master.DoorAnimator.TurnOffAllLights();
			}
		}

		/// <summary>
		/// Set the current state for the bolts light.
		/// </summary>
		/// <param name="enable">True means this door will turn on its bolts lights when the bolts are down</param>
		public void SetBoltsLight(bool enable)
		{
			boltsLights = enable;
		}

		/// <summary>
		/// Debug method to toggle the bolts state. Click the button on inspector while in play mode.
		/// </summary>
		[Button("Toggle bolt state")]
		private void ToggleBoltState()
		{
			SetBoltsState(!boltsDown);
		}

		/// <summary>
		/// Debug method to toggle the bolts lights state. Click the button on inspector while in play mode.
		/// </summary>
		[Button("Toggle bolt lights state")]
		private void ToggleBoltsLights()
		{
			SetBoltsLight(!boltsLights);
		}

		public override ModuleSignal OpenInteraction(HandApply interaction)
		{
			return ModuleSignal.Continue;
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction)
		{
			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer)
		{
			return ModuleSignal.Continue;
		}

		public override bool CanDoorStateChange()
		{
			return !boltsDown;
		}
	}
}
