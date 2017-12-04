using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Events
{

    public class EventController<K, V>
    {
        private class Event : UnityEvent<V> { }

        private Dictionary<int, Event> events = new Dictionary<int, Event>();

        private Func<K, int> hashFunction;

        public EventController(Func<K, int> hashFunction = null)
        {
            this.hashFunction = hashFunction;
        }

        public void AddListener(K eventKey, UnityAction<V> listener)
        {
            Event _event;

            var hashKey = calculateHash(eventKey);
            if (!events.TryGetValue(hashKey, out _event))
            {
                _event = new Event();
                events[hashKey] = _event;
            }

            _event.AddListener(listener);
        }

        public void RemoveListener(K eventKey, UnityAction<V> listener)
        {
            var hashKey = calculateHash(eventKey);
            if (events.ContainsKey(hashKey))
            {
                events[hashKey].RemoveListener(listener);
            }
        }

        public void TriggerEvent(K eventKey, V value)
        {
            var hashKey = calculateHash(eventKey);

            if (events.ContainsKey(hashKey))
            {
                events[hashKey].Invoke(value);
            }
        }

        public void Clear()
        {
            foreach (var v in events.Values)
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
            else
            {
                return hashFunction(eventKey);
            }
        }
    }
}