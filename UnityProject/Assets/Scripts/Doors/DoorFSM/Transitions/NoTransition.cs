using Doors.DoorFSM.States;

namespace Doors.DoorFSM.Transitions
{
	public class NoTransition: DoorTransition
	{
		public NoTransition(StateMachine fsm, DoorState nextState) : base(fsm, nextState)
		{
		}

		public override DoorState ExecuteTransition()
		{
			return nextState;
		}
	}
}