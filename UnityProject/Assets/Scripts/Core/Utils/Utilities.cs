using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace Core.Utils
{

	public class MindNIPossessingEvent : UnityEvent<Mind, IPlayerPossessable> { }


	[System.Serializable]
	public class MultiInterestBool
	{
		[Serializable]
		public class BoolEvent : UnityEvent<bool> { }

		public bool State => state;

		private bool state;

		private RegisterBehaviour Behaviour = RegisterBehaviour.RemoveFalse;

		private BoolBehaviour SetBoolBehaviour = BoolBehaviour.ReturnOnTrue;

		public enum RegisterBehaviour
		{
			RemoveFalse,
			RegisterFalse
		}

		public enum BoolBehaviour
		{
			ReturnOnFalse,
			ReturnOnTrue
		}

		public bool initialState => InitialState;

		[SerializeField] private bool InitialState;

		public Dictionary<object, bool> InterestedParties = new Dictionary<object ,bool>();

		public BoolEvent OnBoolChange = new BoolEvent();

		public void RemovePosition(object Instance)
		{
			if (InterestedParties.ContainsKey(Instance))
			{
				InterestedParties.Remove(Instance);
			}
			RecalculateBoolCash();
		}

		public void RecordPosition(object Instance, bool Position)
		{
			if (Position || Behaviour == RegisterBehaviour.RegisterFalse)
			{
				InterestedParties[Instance] = Position;
			}
			else
			{
				if (InterestedParties.ContainsKey(Instance))
				{
					InterestedParties.Remove(Instance);
				}
			}

			RecalculateBoolCash();
		}

		private void RecalculateBoolCash()
		{
			bool? Tracked = null;

			foreach (var Position in InterestedParties)
			{
				if (Position.Value)
				{
					Tracked = Position.Value;
					if (SetBoolBehaviour == BoolBehaviour.ReturnOnTrue)
					{
						if (state == false)
						{
							state = true;
							OnBoolChange.Invoke(state);
						}

						return;
					}
				}
				else
				{
					Tracked = Position.Value;
					if (SetBoolBehaviour == BoolBehaviour.ReturnOnFalse)
					{
						if (state == true)
						{
							state = false;
							OnBoolChange?.Invoke(state);
						}

						return;
					}
				}
			}

			if (Tracked != null)
			{
				if (Tracked.Value != state)
				{
					state = Tracked.Value;
					OnBoolChange?.Invoke(state);
				}
				return;
			}



			if (state != InitialState)
			{
				state = InitialState;
				OnBoolChange?.Invoke(state);
			}

		}

		public static implicit operator bool(MultiInterestBool value)
		{
			return value.State;
		}

		public MultiInterestBool(bool InInitialState = false,
			RegisterBehaviour inRegisterBehaviour = RegisterBehaviour.RemoveFalse,
			BoolBehaviour InSetBoolBehaviour = BoolBehaviour.ReturnOnTrue)
		{
			InitialState = InInitialState;
			Behaviour = inRegisterBehaviour;
			SetBoolBehaviour = InSetBoolBehaviour;
		}

	}
}


