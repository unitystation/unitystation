using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;

public class InputManagerWrapper : MonoBehaviour
{
	private static HashSet<KeyCode> HeldKeys = new HashSet<KeyCode>();
	private static HashSet<KeyCode> DownKeys = new HashSet<KeyCode>();
	private static HashSet<KeyCode> UpKeys = new HashSet<KeyCode>();

	private static HashSet<KeyCode> ToRemoveUpKeys = new HashSet<KeyCode>();

	private static HashSet<KeyCode> ToRemoveDownKeys = new HashSet<KeyCode>();

	public static Vector3? MousePosition = null;


	private static bool CustomKey = false;


	public static Vector3 GetMousePosition()
	{
		if (Manager3D.Is3D && Input.GetKey(KeyCode.E) == false &&
		    Application.isFocused)
		{
			// Get the center point of the screen
			return new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
		}

		if (MousePosition != null)
		{
			return MousePosition.Value;
		}
		else
		{
			return Input.mousePosition;
		}

	}

	public static void PressKey(KeyCode Key)
	{
		DownKeys.Add(Key);
		HeldKeys.Add(Key);
	}

	public static void UnPressKey(KeyCode Key)
	{
		UpKeys.Add(Key);
	}


	public void OnEnable()
	{
		HeldKeys.Clear();
		DownKeys.Clear();
		UpKeys.Clear();
		CustomKey = false;
		MousePosition = null;
	}

	public void OnDisable()
	{
		HeldKeys.Clear();
		DownKeys.Clear();
		UpKeys.Clear();
		CustomKey = false;
		MousePosition = null;
	}

	public void LateUpdate()
	{
		if (UpKeys.Count > 0)
		{
			foreach (var key in UpKeys)
			{
				HeldKeys.Remove(key);
				DownKeys.Remove(key);
			}
		}

		foreach (var key in ToRemoveDownKeys)
		{
			DownKeys.Remove(key);
		}
		ToRemoveDownKeys.Clear();

		ToRemoveDownKeys.UnionWith(DownKeys);

		foreach (var key in ToRemoveUpKeys)
		{
			UpKeys.Remove(key);
		}
		ToRemoveUpKeys.Clear();

		ToRemoveUpKeys.UnionWith(UpKeys);

		if (HeldKeys.Count > 0 || UpKeys.Count > 0 || DownKeys.Count > 0)
		{
			CustomKey = true;
		}
		else
		{
			CustomKey = false;
		}
	}


	public static bool GetKey(KeyCode key)
	{
		if (CustomKey)
		{
			if (HeldKeys.Contains(key))
			{
				return true;
			}
		}
		return Input.GetKey(key);
	}


	public static bool GetKeyUp(KeyCode key)
	{
		if (CustomKey)
		{
			if (UpKeys.Contains(key))
			{
				return true;
			}
		}
		return Input.GetKeyUp(key);
	}


	public static bool GetKeyDown(KeyCode key)
	{
		if (CustomKey)
		{
			if (DownKeys.Contains(key))
			{
				return true;
			}
		}
		return Input.GetKeyDown(key);
	}


	public static KeyCode MouseIntToKeyCode(int buttonNumber)
	{
		switch (buttonNumber)
		{
			case 0: //Left click
				return KeyCode.Mouse0;
			case 1: //Right click
				return KeyCode.Mouse1;
			case 2: //Middle click, //idk After this
				return KeyCode.Mouse2;
			case 3:
				return KeyCode.Mouse3;
			case 4:
				return KeyCode.Mouse4;
			case 5:
				return KeyCode.Mouse5;
			case 6:
				return KeyCode.Mouse6;
			default:
				Loggy.LogWarning("oh look here someone with a fancy mouse, I couldn't possibly support this (Runs out of enum Values)");
				return KeyCode.None;
		}
	}


	public static bool GetMouseButtonUp(int buttonNumber)
	{
		if (CustomKey)
		{
			var key = MouseIntToKeyCode(buttonNumber);

			if (key != KeyCode.None)
			{
				return GetKeyUp(key);
			}
		}

		return Input.GetMouseButtonUp(buttonNumber);
	}

	public static bool GetMouseButtonDown(int buttonNumber)
	{
		if (CustomKey)
		{
			var key = MouseIntToKeyCode(buttonNumber);

			if (key != KeyCode.None)
			{
				return GetKeyDown(key);
			}
		}

		return Input.GetMouseButtonDown(buttonNumber);
	}

	public static bool GetMouseButton(int buttonNumber)
	{
		if (CustomKey)
		{
			var key = MouseIntToKeyCode(buttonNumber);

			if (key != KeyCode.None)
			{
				return GetKey(key);
			}
		}

		return Input.GetMouseButton(buttonNumber);
	}

}
