using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.Core.Events
{
	public class OnPointerEvent<T> : UnityEvent<PointerEventData, T> {}

	/// <summary>
	/// Used in cases where you need the ability to be able to listen to any pointer event from some class(es). A UI
	/// behaviour then only needs to implement the interfaces it requires and invoke the event type.
	/// </summary>
	/// <typeparam name="T">The type that will be passed along with PointerEventData to the listener.</typeparam>
    public class PointerEventsListener<T>
    {
        private readonly Dictionary<PointerEventType, OnPointerEvent<T>> events = new Dictionary<PointerEventType, OnPointerEvent<T>>();

        private OnPointerEvent<T> Get(PointerEventType eventType)
        {
            if (!events.ContainsKey(eventType))
            {
                events[eventType] = new OnPointerEvent<T>();
            }

            return events[eventType];
        }

        public void AddListener(PointerEventType eventType, UnityAction<PointerEventData, T> callback) =>
            Get(eventType).AddListener(callback);

        public void RemoveListener(PointerEventType eventType, UnityAction<PointerEventData, T> callback) =>
            Get(eventType).RemoveListener(callback);

        public void RemoveAllListeners(PointerEventType eventType) =>
            Get(eventType).RemoveAllListeners();

        public void Invoke(PointerEventType eventType, PointerEventData eventData, T item) =>
            Get(eventType).Invoke(eventData, item);
    }
}
