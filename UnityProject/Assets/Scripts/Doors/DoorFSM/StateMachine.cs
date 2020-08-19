using System;
using System.Threading.Tasks;
using Doors.DoorFSM.States;
using Doors.DoorFSM.Transitions;
using UnityEngine;

namespace Doors.DoorFSM
{
	public class StateMachine
	{
		private DoorState currentState;
		public DoorState CurrentState => currentState;

		private bool isBusy = false;

		private GameObject door;
		public GameObject Door => door;

		private DoorAnimatorV2 doorAnimatorV2;
		public DoorAnimatorV2 DoorAnimatorV2 => doorAnimatorV2;

		private RegisterDoor registerDoor;
		public RegisterDoor RegisterDoor => registerDoor;

		private AccessRestrictions accessRestrictions;
		public AccessRestrictions AccessRestrictions => accessRestrictions;

		private DoorProperties properties;
		private Matrix matrix;

		public DoorProperties Properties => properties;

		private DoorState openState;
		private DoorState closedState;
		private DoorState unpoweredOpenState;
		private DoorState unpoweredClosedState;
		private DoorState emaggedState;

		public DoorState OpenState => openState;
		public DoorState ClosedState => closedState;
		public DoorState EmaggedState => emaggedState;

		public readonly float InitialAutoCloseTime;
		public readonly string NormalUnityLayer;

		public StateMachine(GameObject gameObject, DoorProperties doorProperties)
		{
			door = gameObject;
			properties = doorProperties;
			InitialAutoCloseTime = properties.AutoCloseTime;

			NormalUnityLayer = LayerMask.LayerToName(door.layer);
			doorAnimatorV2 = door.GetComponent<DoorAnimatorV2>();
			registerDoor = door.GetComponent<RegisterDoor>();
			accessRestrictions = door.GetComponent<AccessRestrictions>();
			matrix = registerDoor.Matrix;

			openState = new Open(this);
			closedState = new Closed(this);
			// emaggedState = new Emagged(this);

			// Set initial state
			currentState = closedState;
		}

		public async Task AutoClose()
		{
			if (properties.IsAutomatic == false)
			{
				return;
			}

			await Task.Delay(TimeSpan.FromSeconds(properties.AutoCloseTime));

			if (registerDoor.IsClosed || !properties.HasPower || properties.HasBoltsDown)
			{
				return;
			}

			NextState(new Closing(this, currentState, ClosedState));
		}

		public void NextState(DoorTransition transition)
		{
			if (isBusy)
			{
				return;
			}

			isBusy = true;
			currentState = transition.ExecuteTransition();
			isBusy = false;
		}
	}
}