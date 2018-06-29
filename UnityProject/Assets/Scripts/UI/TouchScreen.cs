using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchScreen : Selectable {
	public override void OnPointerUp( PointerEventData eventData ) {
		base.OnPointerUp( eventData );
		LastTouchPosition = eventData.pressPosition - (Vector2)( ( RectTransform ) transform ).position; //fixme: wrong
		ExecuteOnTouch.Invoke();
	}

//	public PointerEventData LastTouch;
	public Vector2 LastTouchPosition;

	public UnityEvent ExecuteOnTouch;
}