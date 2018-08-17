using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Saves local click position (inside this component) to LastTouchPosition and calls ExecuteOnTouch
/// </summary>
public class TouchScreen : Selectable {
	public override void OnPointerUp( PointerEventData eventData ) {
		base.OnPointerUp( eventData );
		if ( interactable ) {
			//UIManager scaling is important here
			LastTouchPosition = 
				( eventData.pressPosition - (Vector2)((RectTransform) transform).position ) / UIManager.Instance.transform.localScale.x;
			ExecuteOnTouch.Invoke();
		}
	}

//	public PointerEventData LastTouch;
	[HideInInspector]
	public Vector2 LastTouchPosition;

	public UnityEvent ExecuteOnTouch;
}