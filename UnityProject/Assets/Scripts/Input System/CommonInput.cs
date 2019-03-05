using UnityEngine;

/// <summary>
/// Barebones wrapper class for Input that takes virtual gamepad in mind. Work in progress
/// </summary>
public class CommonInput
{
	public static bool GetKeyDown(KeyCode key)
	{
		return Input.GetKeyDown(key) || (UIManager.GamePad && UIManager.GamePad.GetKeyDown(key));
	}
	public static bool GetKey(KeyCode key)
	{
		return Input.GetKey(key) || (UIManager.GamePad && UIManager.GamePad.GetKey(key));
	}
	public static bool GetKeyUp(KeyCode key)
	{
		return Input.GetKeyUp(key) || (UIManager.GamePad && UIManager.GamePad.GetKeyUp(key));
	}

	public static bool IsTouchscreen = Input.touchSupported && Input.multiTouchEnabled;

	public static Vector3 mousePosition
	{
		get
		{
			if ( IsTouchscreen && Input.touchCount > 0 )
			{
				return Input.GetTouch( Input.touchCount - 1 ).position;
			}
			return Input.mousePosition;
		}
	}

	public static bool GetMouseButtonDown( int buttonNumber ) //todo special case for rightclick: emulate it if touch is Still for 700ms
	{
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
			bool mouseButtonDown = Input.GetTouch( Input.touchCount - 1 ).phase == TouchPhase.Began;
			if ( mouseButtonDown )
			{
				Logger.LogTraceFormat( "Touch Mouse button {0} has Begun", Category.UI, buttonNumber );
			}
			return mouseButtonDown; 
		}
		return Input.GetMouseButtonDown( buttonNumber );
	}

	public static bool GetMouseButtonUp( int buttonNumber )
	{
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
			bool mouseButtonUp = Input.GetTouch( Input.touchCount - 1 ).phase == TouchPhase.Ended;
			if ( mouseButtonUp )
			{
				Logger.LogTraceFormat( "Touch Mouse button {0} has Ended", Category.UI, buttonNumber );
			}
			return mouseButtonUp;
		}
		return Input.GetMouseButtonUp( buttonNumber );
	}

	public static bool GetMouseButton( int buttonNumber )
	{
		if ( IsTouchscreen && Input.touchCount > 0 && buttonNumber == 0 )
		{
//			Logger.LogTraceFormat( "Touch Mouse button {0} is pressed", Category.UI, buttonNumber );
			return true;
		}
		return Input.GetMouseButton( buttonNumber );
	}
}