using Doors.DoorFSM.Transitions;
using UnityEngine;

namespace Doors.DoorFSM.States
{
	public class Open: DoorState
	{
		public Open(StateMachine fsm) : base(fsm)
		{
		}

		public override DoorTransition TryOpen(GameObject byPlayer, bool force)
		{
			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryClose(GameObject byPlayer)
		{
			if (UnableToMove())
			{
				return new NoTransition(fsm, fsm.CurrentState);
			}

			return new Closing(fsm, fsm.CurrentState, fsm.ClosedState);
		}

		public override DoorTransition TryBolts(GameObject byPlayer)
		{
			ChangeBoltState();

			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryBoltLights(GameObject byPlayer)
		{
			fsm.Properties.HasBoltLights = !fsm.Properties.HasBoltLights;
			return new NoTransition(fsm,  fsm.CurrentState);
		}

		public override DoorTransition TryWelder(Intent intent, GameObject byPlayer)
		{
			Chat.AddActionMsgToChat(byPlayer, "You can't weld this airlock open.", $"");
			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryPanel(GameObject byPlayer)
		{
			Chat.AddActionMsgToChat(
				byPlayer,
				"You can't reach the panel because this airlock is open!",
				"");

			return new NoTransition(fsm, fsm.CurrentState);
		}

		public override DoorTransition TryCrowbar(GameObject byPlayer)
		{
			Chat.AddActionMsgToChat(
				byPlayer,
				"You try to force close this airlock but the mechanism doesn't give in!",
				$"{byPlayer.ExpensiveName()} tries to force close this door to no avail.");

			return new NoTransition(fsm, fsm.CurrentState);
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
			switch (states)
			{
				case PowerStates.On:
					fsm.Properties.AutoCloseTime = fsm.InitialAutoCloseTime;
					fsm.Properties.HasPower = true;
					break;
				case PowerStates.Off:
					fsm.Properties.HasPower = false;
					break;
				case PowerStates.LowVoltage:
					fsm.Properties.AutoCloseTime *= 2;
					break;
				case PowerStates.OverVoltage:
					fsm.Properties.AutoCloseTime /= 2;
					break;
			}

			return new NoTransition(fsm, fsm.CurrentState);
		}
	}
}