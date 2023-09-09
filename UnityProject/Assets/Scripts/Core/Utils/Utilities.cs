using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = System.Random;


namespace Core.Utils
{

	public static class Utils
	{
		private static Random random = new Random();
		public static void SetValueByName(this Dropdown dropdown, string valueName)
		{
			List<Dropdown.OptionData> options = dropdown.options;
			for (int i = 0; i < options.Count; i++)
			{
				if (options[i].text == valueName)
				{
					dropdown.value = i;
					break;
				}
			}
		}

		public static string GetValueName(this Dropdown dropdown)
		{
			List<Dropdown.OptionData> options = dropdown.options;
			int selectedIndex = dropdown.value;
			if (selectedIndex >= 0 && selectedIndex < options.Count)
			{
				return options[selectedIndex].text;
			}
			return null;
		}

		public static T[] FindAll<T>(this T[] items, Predicate<T> predicate) => Array.FindAll<T>(items, predicate);
		public static T PickRandom<T>(this IEnumerable<T> source)
		{
			return source.PickRandom(1).SingleOrDefault();
		}

		public static T PickRandomNonNull<T>(this IList<T> source)
		{
			if (source == null || source.Count == 0)
			{
				throw new InvalidOperationException("The list is empty or null.");
			}

			var nonNullItems = source.Where(item => item != null).ToList();

			if (nonNullItems.Count == 0)
			{
				throw new InvalidOperationException("There are no non-null elements in the list.");
			}

			int randomIndex = random.Next(nonNullItems.Count);
			return nonNullItems[randomIndex];
		}
	}

	#if UNITY_EDITOR
	public static class DEBUG
	{
		public static bool RUN(Action Action)
		{
			Action.Invoke();
			return true;
		}
	}
	#endif


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
			RemoveFalse, //Your entry of false will be just removed from the dictionary and won't contribute to
			RegisterFalse //Your entry will be registered as false in the dictionary and contribute to options
		}

		public enum BoolBehaviour
		{
			ReturnOnFalse, //if any value is false Overrides all
			ReturnOnTrue //If any value is true overrides all
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

		public MultiInterestBool(bool InDefaultState = false,
			RegisterBehaviour inRegisterBehaviour = RegisterBehaviour.RemoveFalse,
			BoolBehaviour InSetBoolBehaviour = BoolBehaviour.ReturnOnTrue)
		{
			InitialState = InDefaultState;
			state = InitialState;
			Behaviour = inRegisterBehaviour;
			SetBoolBehaviour = InSetBoolBehaviour;
		}

	}

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


