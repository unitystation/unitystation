﻿using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// Input field that would properly focus
/// and ignore movement and whatnot while it's focused
[Serializable]
public class InputFieldFocus : InputField
{
	/// <summary>
	/// Button that will cause the field to lose focus
	/// </summary>
	public KeyCode ExitButton = KeyCode.Escape;

//disabling auto focus on enable temporarily because it causes NREs
//	protected override void OnEnable() {
//		base.OnEnable();
//		StartCoroutine( SelectDelayed() );
//	}
	/// Waiting one frame to init
	private IEnumerator SelectDelayed() {
		yield return WaitFor.EndOfFrame;
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
		if ( Event.current.keyCode == ExitButton ) {
			OnDeselect(new BaseEventData( EventSystem.current ));
		}
	}
}