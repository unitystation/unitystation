
using UnityEngine.Events;

/// <summary>
/// An action subscribed to a particular unity event. Overloads for events with multiple
/// parameters.
/// </summary>
public class SubscribedAction : IUnsubscribable
{
	private readonly UnityAction action;
	private readonly UnityEvent unityEvent;

	public SubscribedAction(UnityEvent unityEvent, UnityAction action)
	{
		this.action = action;
		this.unityEvent = unityEvent;
	}

	public void Unsubscribe()
	{
		unityEvent.RemoveListener(action);
	}
}

public class SubscribedAction<T> : IUnsubscribable
{
	private readonly UnityAction<T> action;
	private readonly UnityEvent<T> unityEvent;

	public SubscribedAction(UnityEvent<T> unityEvent, UnityAction<T> action)
	{
		this.action = action;
		this.unityEvent = unityEvent;
	}

	public void Unsubscribe()
	{
		unityEvent.RemoveListener(action);
	}
}

public class SubscribedAction<T, T2> : IUnsubscribable
{
	private readonly UnityAction<T, T2> action;
	private readonly UnityEvent<T, T2> unityEvent;

	public SubscribedAction(UnityEvent<T, T2> unityEvent, UnityAction<T, T2> action)
	{
		this.action = action;
		this.unityEvent = unityEvent;
	}

	public void Unsubscribe()
	{
		unityEvent.RemoveListener(action);
	}
}

public class SubscribedAction<T, T2, T3> : IUnsubscribable
{
	private readonly UnityAction<T, T2, T3> action;
	private readonly UnityEvent<T, T2, T3> unityEvent;

	public SubscribedAction(UnityEvent<T, T2, T3> unityEvent, UnityAction<T, T2, T3> action)
	{
		this.action = action;
		this.unityEvent = unityEvent;
	}

	public void Unsubscribe()
	{
		unityEvent.RemoveListener(action);
	}
}
