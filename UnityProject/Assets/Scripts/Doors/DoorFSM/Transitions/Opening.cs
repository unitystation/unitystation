using System.Collections;
using Doors.DoorFSM.States;
using UnityEngine;

namespace Doors.DoorFSM.Transitions
{
	public class Opening: DoorTransition
	{
		public Opening(StateMachine fsm, DoorState nextState) : base(fsm, nextState)
		{
		}

		private IEnumerator OpeningAnimation()
		{
			UpdatePassable(OPEN_DOOR_LAYER);
			bool panel = fsm.Properties.HasPanelExposed;
			bool lights = fsm.Properties.HasPower;
			yield return fsm.DoorAnimatorV2.PlayOpeningAnimation(panel, lights);
		}

		public override DoorState ExecuteTransition()
		{
			fsm.DoorAnimatorV2.RequestAnimation(OpeningAnimation());
			AutoClose();
			return nextState;
		}

		private async void AutoClose()
		{
			await fsm.AutoClose();
		}
	}
}