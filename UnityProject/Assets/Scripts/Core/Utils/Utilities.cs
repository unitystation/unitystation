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


		public bool initialState => InitialState;

		[SerializeField] private bool InitialState;

		public Dictionary<object, bool> InterestedParties = new Dictionary<object ,bool>();

		public BoolEvent OnBoolChange;

		public void RecordPosition(object Instance, bool Position)
		{
			if (Position)
			{
				InterestedParties[Instance] = true;
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
			foreach (var Position in InterestedParties)
			{
				if (Position.Value)
				{
					if (state == false)
					{
						state = true;
						OnBoolChange.Invoke(state);
					}

					return;
				}
			}

			if (state)
			{
				state = false;
				OnBoolChange.Invoke(state);
			}

		}

		public static implicit operator bool(MultiInterestBool value)
		{
			return value.State;
		}
	}
}


