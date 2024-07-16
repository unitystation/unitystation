using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Utils
{
	public class MultiInterestFloat
	{
		[Serializable]
		public class FloatEvent : UnityEvent<float> { }

		public float State => state;

		private float state;

		private RegisterBehaviour Behaviour = RegisterBehaviour.Remove0;

		private FloatBehaviour SetFloatBehaviour = FloatBehaviour.ReturnOn1;

		public enum RegisterBehaviour
		{
			Remove0, //Your entry of 0 will be just removed from the dictionary and won't contribute to
			Register0 //Your entry will be registered as false in the dictionary and contribute to options
		}

		public enum FloatBehaviour
		{
			ReturnOn0, //if any value is 0 Overrides all
			ReturnOn1 //if any value is 1 overrides all
		}
		public float initialState => InitialState;

		[SerializeField] private float InitialState;

		public Dictionary<object, float> InterestedParties = new Dictionary<object ,float>();

		public FloatEvent OnFloatChange = new FloatEvent();

		public void RemovePosition(object Instance)
		{
			if (InterestedParties.ContainsKey(Instance))
			{
				InterestedParties.Remove(Instance);
			}
			RecalculateBoolCash();
		}

		public void RecordPosition(object Instance, float Position)
		{
			if (Position != 0 || Behaviour == RegisterBehaviour.Register0)
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
			float? Tracked = null;

			foreach (var Position in InterestedParties)
			{
				if (Position.Value > 0)
				{
					Tracked = Position.Value;

					if (Tracked.Value >= 1f && SetFloatBehaviour == FloatBehaviour.ReturnOn1)
					{
						if (state != Tracked.Value)
						{
							state = Tracked.Value;
							OnFloatChange?.Invoke(state);
						}

						return;
					}
				}
				else
				{
					if (Tracked != null)
					{
						if (Position.Value > Tracked.Value)
						{
							Tracked = Position.Value;
						}
					}
					else
					{
						Tracked = Position.Value;
					}


					if (SetFloatBehaviour == FloatBehaviour.ReturnOn0)
					{
						if (state != Tracked.Value)
						{
							state = Tracked.Value;
							OnFloatChange?.Invoke(state);
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
					OnFloatChange?.Invoke(state);
				}
				return;
			}



			if (state != InitialState)
			{
				state = InitialState;
				OnFloatChange?.Invoke(state);
			}

		}

		public static implicit operator float(MultiInterestFloat value)
		{
			return value.State;
		}

		public MultiInterestFloat(float InInitialState = 0,
			RegisterBehaviour inRegisterBehaviour = RegisterBehaviour.Remove0,
			FloatBehaviour InSetFloatBehaviour = FloatBehaviour.ReturnOn1)
		{
			InitialState = InInitialState;
			Behaviour = inRegisterBehaviour;
			SetFloatBehaviour  = InSetFloatBehaviour;
		}
	}
}