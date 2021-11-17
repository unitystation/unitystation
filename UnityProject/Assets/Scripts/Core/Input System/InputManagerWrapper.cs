using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManagerWrapper : MonoBehaviour
{
	private static HashSet<KeyCode> HeldKeys = new HashSet<KeyCode>();
	private static HashSet<KeyCode> DownKeys = new HashSet<KeyCode>();
	private static HashSet<KeyCode> UpKeys = new HashSet<KeyCode>();

	public static bool CustomKey = false;

	public static void PressKey(KeyCode Key)
	{
		DownKeys.Add(Key);
		HeldKeys.Add(Key);
	}

	public static void UnPressKey(KeyCode Key)
	{
		UpKeys.Add(Key);
	}


	public void LateUpdate()
	{
		if (UpKeys.Count > 0)
		{
			foreach (var key in UpKeys)
			{
				HeldKeys.Remove(key);
			}
		}

		DownKeys.Clear();
		UpKeys.Clear();

		if (HeldKeys.Count > 0)
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
}
