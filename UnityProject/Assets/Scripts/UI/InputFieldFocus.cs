using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Input field that would properly focus
/// and ignore movement and whatnot while it's focused
public class InputFieldFocus : InputField
{
	protected override void OnEnable() {
		base.OnEnable();
		StartCoroutine( SelectDelayed() );
	}
	/// Waiting one frame to init
	private IEnumerator SelectDelayed() {
		yield return new WaitForEndOfFrame();
		Select();
	}

	protected override void OnDisable() {
		base.OnDisable();
		UIManager.IsInputFocus = false;
	}

	public override void OnSelect( BaseEventData eventData ) {
		base.OnSelect( eventData );
		UIManager.IsInputFocus = true;
	}

	public override void OnPointerClick( PointerEventData eventData ) {
		base.OnPointerClick( eventData );
		UIManager.IsInputFocus = true;
	}

	public override void OnDeselect( BaseEventData eventData ) {
		base.OnDeselect( eventData );
		UIManager.IsInputFocus = false;
	}

	public override void OnSubmit( BaseEventData eventData ) {
		base.OnSubmit( eventData );
		UIManager.IsInputFocus = false;
	}

	private void OnGUI() {
		if ( Event.current.keyCode == KeyCode.Escape ) {
			OnDeselect(new BaseEventData( EventSystem.current ));
		}
	}
}