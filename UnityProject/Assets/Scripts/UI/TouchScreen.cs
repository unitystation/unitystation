using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TouchScreen : Selectable {
	public override void OnPointerUp( PointerEventData eventData ) {
		base.OnPointerUp( eventData );
		if ( interactable ) {
			//fixme: wrong
			LastTouchPosition = eventData.pressPosition - (Vector2)( ( RectTransform ) transform ).position;
			ExecuteOnTouch.Invoke();
		}
	}

//	public PointerEventData LastTouch;
	[HideInInspector]
	public Vector2 LastTouchPosition;

	public UnityEvent ExecuteOnTouch;
}