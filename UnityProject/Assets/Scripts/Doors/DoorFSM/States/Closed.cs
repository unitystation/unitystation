using Doors.DoorFSM.Transitions;
using UnityEngine;

namespace Doors.DoorFSM.States
{
	public class Closed: DoorState
	{
		public Closed(StateMachine fsm) : base(fsm)
		{
		}

		public override DoorTransition TryOpen(GameObject byPlayer, bool force)
		{
			if (force)
			{
				return new Opening(fsm, fsm.OpenState);
			}

			if (UnableToMove())
			{
				return new NoTransition(fsm, fsm.CurrentState);
			}

			return new Opening(fsm, fsm.OpenState);
		}

		public override DoorTransition TryClose(GameObject byPlayer)
		{
			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryBolts(GameObject byPlayer)
		{
			ChangeBoltState();

			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryBoltLights(GameObject byPlayer)
		{
			fsm.Properties.HasBoltLights = !fsm.Properties.HasBoltLights;
			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryWelder(Intent intent, GameObject byPlayer)
		{
			throw new System.NotImplementedException();
		}

		public override DoorTransition TryPanel(GameObject byPlayer)
		{
			throw new System.NotImplementedException();
		}

		public override DoorTransition TryCrowbar(GameObject byPlayer)
		{
			throw new System.NotImplementedException();
		}

		public override DoorTransition TryEmag(DoorState doorState)
		{
			throw new System.NotImplementedException();
		}

		public override DoorTransition TryReplaceCB(GameObject byPlayer, GameObject newCircuitBoard)
		{
			throw new System.NotImplementedException();
		}

		public override DoorTransition ChangePower(PowerStates states)
		{
			fsm.Properties.HasPower = !fsm.Properties.HasPower;
			return new NoTransition(fsm, fsm.CurrentState);
		}
	}
}