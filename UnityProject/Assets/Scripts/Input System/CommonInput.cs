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

	public static Vector3 mousePosition => IsTouchscreen ? (Vector3)Input.GetTouch( Input.touchCount ).position : Input.mousePosition;

	public static bool GetMouseButtonDown( int buttonNumber ) //todo special case for rightclick: emulate it if touch is held for 700ms
	{
		return Input.GetMouseButtonDown( buttonNumber );
	}

	public static bool GetMouseButtonUp( int buttonNumber )
	{
		return Input.GetMouseButtonUp( buttonNumber );
	}

	public static bool GetMouseButton( int buttonNumber )
	{
		return Input.GetMouseButton( buttonNumber );
	}
}