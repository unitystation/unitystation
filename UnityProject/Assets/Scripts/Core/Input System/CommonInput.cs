using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Barebones wrapper class for Input that takes virtual gamepad in mind. Work in progress
/// </summary>
public class CommonInput
{
	public static bool GetKeyDown(KeyCode key)
	{
		return InputManagerWrapper.GetKeyDown(key) || (UIManager.GamePad && UIManager.GamePad.GetKeyDown(key));
	}
	public static bool GetKey(KeyCode key)
	{
		return InputManagerWrapper.GetKey(key) || (UIManager.GamePad && UIManager.GamePad.GetKey(key));
	}
	public static bool GetKeyUp(KeyCode key)
	{
		return InputManagerWrapper.GetKeyUp(key) || (UIManager.GamePad && UIManager.GamePad.GetKeyUp(key));
	}

	public static bool IsTouchscreen = Input.touchSupported && Input.multiTouchEnabled;

	public static Vector3 mousePosition
	{
		get
		{
#if UNITY_IOS || UNITY_ANDROID
			if ( IsTouchscreen && Input.touchCount > 0 )
			{
				return GetNonGamePadTouch()?.position ?? -Vector2.one;
			}
#endif
			return InputManagerWrapper.GetMousePosition();
		}
	}

	private static bool gameKeyDetected = false;
	/// <summary>
	/// Get first touch that's NOT a GameKey touch
	/// </summary>
	/// <returns></returns>
	private static Touch? GetNonGamePadTouch()
	{ //ok, this is expensive but it works for now
		foreach (var touch in Input.touches)
		{
			gameKeyDetected = false;
			List<RaycastResult> results = new List<RaycastResult>();

			EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
			{
				position = touch.position,
				pointerId = touch.fingerId
			}, results );

			for (var i = 0; i < results.Count; i++)
			{
				if (results[i].gameObject.GetComponent<GameKey>() != null)
				{
					gameKeyDetected = true;
					break;
				}
			}

			if (!gameKeyDetected)
			{
				return touch;
			}
		}

		return null;
	}

	public static bool GetMouseButtonDown( int buttonNumber ) //todo special case for rightclick: emulate it if touch is Still for 700ms
	{
#if UNITY_IOS || UNITY_ANDROID
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
			bool mouseButtonDown = GetNonGamePadTouch()?.phase == TouchPhase.Began;
			if ( mouseButtonDown )
			{
				Logger.LogTraceFormat( "Touch Mouse button {0} has Begun", Category.UserInput, buttonNumber );
			}
			return mouseButtonDown;
		}
#endif
		return InputManagerWrapper.GetMouseButtonDown( buttonNumber );
	}

	public static bool GetMouseButtonUp( int buttonNumber )
	{
#if UNITY_IOS || UNITY_ANDROID
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
			bool mouseButtonUp = GetNonGamePadTouch()?.phase == TouchPhase.Ended;
			if ( mouseButtonUp )
			{
				Logger.LogTraceFormat( "Touch Mouse button {0} has Ended", Category.UserInput, buttonNumber );
			}
			return mouseButtonUp;
		}
#endif
		return InputManagerWrapper.GetMouseButtonUp( buttonNumber );
	}

	public static bool GetMouseButton( int buttonNumber )
	{
#if UNITY_IOS || UNITY_ANDROID
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
//			Logger.LogTraceFormat( "Touch Mouse button {0} is pressed", Category.UserInput, buttonNumber );
			return GetNonGamePadTouch()?.phase < (TouchPhase?) 3; //(Began/Moved/Stationary)
		}
#endif
		return InputManagerWrapper.GetMouseButton( buttonNumber );
	}
}