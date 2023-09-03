using System;
using Logs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Represents a key or keycombo for virtual gamepad
/// </summary>
public class GameKey : MonoBehaviour,
	IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
	private const float DARKEN = 0.7f;
	public KeyCode[] Keys;
	public UnityEvent OnKeyPress;
	public UnityEvent OnKeyRelease;
	public Image Image;
	private Color srcColor;
	private Color pressColor;

	protected virtual void OnEnable()
	{
		if ( Image == null )
		{
			Image = GetComponentInChildren<Image>();
		}

		srcColor = Image.color;
		pressColor = new Color(
			srcColor.r * DARKEN,
			srcColor.g * DARKEN,
			srcColor.b * DARKEN,
			srcColor.a );
	}

	public override string ToString()
	{
		return String.Join( ",", Keys ) + " (" + gameObject.name + ')';
	}

	/// <summary>
	/// Released
	/// </summary>
	public void OnPointerUp( PointerEventData eventData )
	{
		Loggy.LogTraceFormat( "OnPointerUp {0}", Category.UserInput, eventData );
		OnKeyRelease.Invoke();
		Image.color = srcColor;
	}

	public void OnPointerExit( PointerEventData eventData )
	{
		eventData.pointerPress = null;
		Loggy.LogTraceFormat( "OnPointerExit {0}", Category.UserInput, eventData );
		if ( eventData.eligibleForClick )
		{
			ExecuteEvents.Execute( gameObject, eventData, ExecuteEvents.pointerUpHandler );
		}
	}

	public void OnPointerEnter( PointerEventData eventData )
	{
		if ( eventData.pointerPress == gameObject )
		{
			return;
		}
		Loggy.LogTraceFormat( "OnPointerEnter {0}", Category.UserInput, eventData );
		eventData.pointerPress = gameObject;
		if ( eventData.eligibleForClick )
		{
			ExecuteEvents.Execute( gameObject, eventData, ExecuteEvents.pointerDownHandler );
		}
	}

	/// <summary>
	/// Pressed
	/// </summary>
	public void OnPointerDown( PointerEventData eventData )
	{
		Loggy.LogTraceFormat( "OnPointerDown {0}", Category.UserInput, eventData );
		OnKeyPress.Invoke();
		Image.color = pressColor;
	}
}