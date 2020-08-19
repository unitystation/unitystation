using Doors.DoorFSM.States;
using UnityEngine;

namespace Doors.DoorFSM.Transitions
{
	public abstract class DoorTransition
	{
		protected const string OPEN_DOOR_LAYER = "Door Open";
		protected StateMachine fsm;
		protected DoorState nextState;
		protected AccessRestrictions accessRestrictions;

		protected DoorTransition(StateMachine fsm, DoorState nextState)
		{
			this.fsm = fsm;
			this.nextState = nextState;
		}

		protected void UpdatePassable(string layer)
		{
			fsm.Door.layer = LayerMask.NameToLayer(layer);

			foreach (Transform child in fsm.Door.transform)
			{
				child.gameObject.layer = LayerMask.NameToLayer(layer);
			}

			fsm.RegisterDoor.IsClosed = (fsm.Door.gameObject.layer != LayerMask.NameToLayer(OPEN_DOOR_LAYER));
		}

		public abstract DoorState ExecuteTransition();
	}
}