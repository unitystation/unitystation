using Doors.DoorFSM.Transitions;
using UnityEngine;

namespace Doors.DoorFSM.States
{
	public abstract class DoorState
	{
		protected StateMachine fsm;

		protected DoorState(StateMachine fsm)
		{
			this.fsm = fsm;
		}

		protected void ChangeBoltState()
		{
			fsm.Properties.HasBoltsDown = !fsm.Properties.HasBoltsDown;
			if (fsm.Properties.HasBoltsDown && fsm.Properties.HasPower && fsm.Properties.HasBoltLights)
			{
				fsm.DoorAnimatorV2.ToggleBoltsLights();
			}
		}

		protected bool UnableToMove()
		{
			return !fsm.Properties.HasPower || fsm.Properties.IsWeld || fsm.Properties.HasBoltsDown;
		}

		public abstract DoorTransition TryOpen(GameObject byPlayer, bool force);
		public abstract DoorTransition TryClose(GameObject byPlayer);
		public abstract DoorTransition TryBolts(GameObject byPlayer);
		public abstract DoorTransition TryBoltLights(GameObject byPlayer);
		public abstract DoorTransition TryWelder(Intent intent, GameObject byPlayer);
		public abstract DoorTransition TryPanel(GameObject byPlayer);
		public abstract DoorTransition TryCrowbar(GameObject byPlayer);
		public abstract DoorTransition TryEmag(DoorState doorState);
		public abstract DoorTransition TryReplaceCB(GameObject byPlayer, GameObject newCircuitBoard);
		public abstract DoorTransition ChangePower(PowerStates states);
	}
}