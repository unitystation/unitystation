using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Use this in areas where you need to get around an Event Trigger consuming all the events
/// (i.e. if you add an event trigger to the handle of the scroll bar, the scroll bar does not
/// work anymore. So use custom scripts that use the IPointer interfaces instead)
/// </summary>
public class PointerDownUpTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	public UnityEvent onPointerDownEvent;
	public UnityEvent onPointerUpEvent;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (onPointerDownEvent != null)
		{
			onPointerDownEvent.Invoke();
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (onPointerUpEvent != null)
		{
			onPointerUpEvent.Invoke();
		}
	}
}