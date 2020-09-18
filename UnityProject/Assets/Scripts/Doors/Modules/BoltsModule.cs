using NaughtyAttributes;
using UnityEngine;

namespace Doors.Modules
{
	public class BoltsModule : DoorModuleBase
	{
		private bool boltsDown = false;
		private bool boltsLights = true;

		[SerializeField][Tooltip("If true, the door needs to be closed to see the bolts lights")]
		private bool needsClosedToLight = true;

		private bool CanShowLights
		{
			get
			{
				if (needsClosedToLight)
				{
					return boltsLights && master.HasPower && master.IsClosed;
				}

				return boltsLights && master.HasPower;
			}
		}

		/// <summary>
		/// Set the current state for this door's bolts.
		/// </summary>
		/// <param name="state">True means the bolts are down and the door can't be opened</param>
		[ContextMenu("Set bolt state")]
		public void SetBoltsState(bool state)
		{
			boltsDown = state;

			if (boltsDown && CanShowLights)
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
