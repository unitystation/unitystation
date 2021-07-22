using System;
using System.Collections.Generic;
using UnityEngine.Events;

public class EventController<K, V>
{
	private readonly Dictionary<int, Event> events = new Dictionary<int, Event>();

	private readonly Func<K, int> hashFunction;

	public EventController(Func<K, int> hashFunction = null)
	{
		this.hashFunction = hashFunction;
	}

	public void AddListener(K eventKey, UnityAction<V> listener)
	{
		Event _event;

		int hashKey = calculateHash(eventKey);
		if (!events.TryGetValue(hashKey, out _event))
		{
			_event = new Event();
			events[hashKey] = _event;
		}

		_event.AddListener(listener);
	}

	public void RemoveListener(K eventKey, UnityAction<V> listener)
	{
		int hashKey = calculateHash(eventKey);
		if (events.ContainsKey(hashKey))
		{
			events[hashKey].RemoveListener(listener);
		}
	}

	public void TriggerEvent(K eventKey, V value)
	{
		int hashKey = calculateHash(eventKey);

		if (events.ContainsKey(hashKey))
		{
			events[hashKey].Invoke(value);
		}
	}

	public void Clear()
	{
		foreach (Event v in events.Values)
		{
			v.RemoveAllListeners();
		}
	}

	private int calculateHash(K eventKey)
	{
		if (hashFunction == null)
		{
			return eventKey.GetHashCode();
		}
		return hashFunction(eventKey);
	}

	private class Event : UnityEvent<V>	{}
}
