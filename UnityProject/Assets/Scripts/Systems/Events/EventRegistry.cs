
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// EventRegistry can be used in a class to simplify the management of subscribing to multiple
/// event hooks and ensuring that they are all unsubscribed from when desired. This helps to avoid
/// the common mistake of subscribing to a bunch of events and later unsubscribing to them all but
/// forgetting a few. It also allows subscribing with lambdas / anonymous functions.
///
/// Simply call Register to subscribe to events, then when you want to unsubscribe from everything,
/// call UnregisterAll to unsubscribe all the events that had been subscribed.
/// </summary>
public class EventRegistry
{
	private readonly List<IUnsubscribable> subscriptions = new List<IUnsubscribable>();

	/// <summary>
	/// Subscribe to the event. Will be unregistered when EventRegistry.UnregisterAll is
	/// called later.
	/// </summary>
	public void Register(UnityEvent unityEvent, UnityAction unityAction)
	{
		if (unityEvent == null) return;
		if (unityAction == null) return;
		unityEvent.AddListener(unityAction);
		subscriptions.Add(new SubscribedAction(unityEvent, unityAction));
	}

	/// <summary>
	/// Subscribe to the event. Will be unregistered when EventRegistry.UnregisterAll is
	/// called later.
	/// </summary>
	public void Register<T>(UnityEvent<T> unityEvent, UnityAction<T> unityAction)
	{
		unityEvent.AddListener(unityAction);
		subscriptions.Add(new SubscribedAction<T>(unityEvent, unityAction));
	}

	/// <summary>
	/// Subscribe to the event. Will be unregistered when EventRegistry.UnregisterAll is
	/// called later.
	/// </summary>
	public void Register<T, T2>(UnityEvent<T, T2> unityEvent, UnityAction<T, T2> unityAction)
	{
		unityEvent.AddListener(unityAction);
		subscriptions.Add(new SubscribedAction<T, T2>(unityEvent, unityAction));
	}

	/// <summary>
	/// Subscribe to the event. Will be unregistered when EventRegistry.UnregisterAll is
	/// called later.
	/// </summary>
	public void Register<T, T2, T3>(UnityEvent<T, T2, T3> unityEvent, UnityAction<T, T2, T3> unityAction)
	{
		unityEvent.AddListener(unityAction);
		subscriptions.Add(new SubscribedAction<T, T2, T3>(unityEvent, unityAction));
	}

	/// <summary>
	/// Unsubscribes all unityactions that have been previously subscribed using EventRegistry.Register.
	/// </summary>
	public void UnregisterAll()
	{
		foreach (var subscribedListener in subscriptions)
		{
			subscribedListener.Unsubscribe();
		}
		subscriptions.Clear();
	}



}
