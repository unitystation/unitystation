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
	/// <summary>
	/// if isFocused is not working then try this IsFocused property. It is updated via the
	/// Disable and Enable Input events on InputFieldFocus
	/// </summary>
	public bool IsFocused { get; private set; }

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
		if (UIManager.Instance == null) return;
		UIManager.IsInputFocus = true;
		UIManager.PreventChatInput = true;
		IsFocused = true;
	}

	private void EnableInput()
	{
		if (UIManager.Instance == null) return;
		UIManager.IsInputFocus = false;
		UIManager.PreventChatInput = false;
		IsFocused = false;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		IsFocused = false;
		if(gameObject.activeInHierarchy)
			StartCoroutine(DelayedEnableInput());
		else
			EnableInput();
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

	/// <summary>
	/// This event is called when the input field is deselected.
	/// </summary>
	/// <param name="eventData">Data for the event</param>
	public override void OnDeselect( BaseEventData eventData )
	{
		base.OnDeselect( eventData );
		if (!gameObject.activeInHierarchy)
		{
			EnableInput();
		}
		else
		{
			StartCoroutine(DelayedEnableInput());
		}
	}

	public override void OnSubmit( BaseEventData eventData )
	{
		base.OnSubmit( eventData );
		StartCoroutine(DelayedEnableInput());
	}

	private void OnGUI()
	{
		if ( Event.current.keyCode == ExitButton )
		{
			OnDeselect(new BaseEventData( EventSystem.current ));
		}
	}
}