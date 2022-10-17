using System;

namespace Util.Rx
{
	/// <summary>
	/// Implementation of BehaviourSubject from Angular.
	/// Object that allows for reactivity, receives an initial value and can be subscribed to. Emits new value to subscribers when value is changed.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class BehaviourSubject<T>
	{
		public BehaviourSubject(T initialValue)
		{
			Value = initialValue;
		}

		private event Action<T> ValueChanged;
		public T Value { get; private set; }

		public void Subscribe(Action<T> function)
		{
			ValueChanged += function;
		}

		public void Unsubscribe(Action<T> function)
		{
			ValueChanged -= function;
		}

		public void Next(T newValue)
		{
			Value = newValue;
			ValueChanged?.Invoke(newValue);
		}
	}
}