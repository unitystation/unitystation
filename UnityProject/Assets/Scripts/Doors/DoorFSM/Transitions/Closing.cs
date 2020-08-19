using System.Collections;
using Doors.DoorFSM.States;

namespace Doors.DoorFSM.Transitions
{
	public class Closing: DoorTransition
	{
		public Closing(StateMachine fsm, DoorState currState, DoorState nextState) : base(fsm, nextState)
		{
		}

		private IEnumerator ClosingAnimation()
		{
			bool panel = fsm.Properties.HasPanelExposed;
			bool lights = fsm.Properties.HasPower;
			yield return fsm.DoorAnimatorV2.PlayClosingAnimation(panel, lights);
			UpdatePassable(fsm.NormalUnityLayer);
		}

		public override DoorState ExecuteTransition()
		{
			fsm.DoorAnimatorV2.RequestAnimation(ClosingAnimation());
			return nextState;
		}
	}
}