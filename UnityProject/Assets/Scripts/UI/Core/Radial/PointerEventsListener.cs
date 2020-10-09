using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UI.Core.Radial
{
	public enum PointerEventType
	{
		PointerEnter,
		PointerExit,
		PointerClick,
		BeginDrag,
		Drag,
		EndDrag,
		Select,
		Deselect,
	}

	public class OnPointerEvent<T> : UnityEvent<PointerEventData, T> {}

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
