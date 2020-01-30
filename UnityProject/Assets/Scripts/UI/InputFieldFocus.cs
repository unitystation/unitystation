using System;
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
	private IEnumerator SelectDelayed()
	{
		yield return WaitFor.EndOfFrame;
		Select();
	}

	private IEnumerator DelayedEnableInput()
	{
		yield return WaitFor.EndOfFrame;
		EnableInput();
	}

	private void DisableInput()
	{
		UIManager.IsInputFocus = true;
		UIManager.PreventChatInput = true;
	}

	private void EnableInput()
	{
		UIManager.IsInputFocus = false;
		UIManager.PreventChatInput = false;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		StartCoroutine(DelayedEnableInput());
	}

	public override void OnSelect( BaseEventData eventData )
	{
		base.OnSelect( eventData );
		DisableInput();
	}

	public override void OnPointerClick( PointerEventData eventData )
	{
		base.OnPointerClick( eventData );
		DisableInput();
	}

	public override void OnDeselect( BaseEventData eventData )
	{
		base.OnDeselect( eventData );
		StartCoroutine(DelayedEnableInput());
	}

	public override void OnSubmit( BaseEventData eventData )
	{
		base.OnSubmit( eventData );
		_ = StartCoroutine(DelayedEnableInput());
	}

	private void OnGUI()
	{
		if ( Event.current.keyCode == ExitButton )
		{
			OnDeselect(new BaseEventData( EventSystem.current ));
		}
	}
}