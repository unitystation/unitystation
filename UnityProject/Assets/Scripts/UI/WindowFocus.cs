using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// use to focus InputBoxes
/// </summary>
public class WindowFocus : MonoBehaviour
{
	private InputField Searchtext;
	private void Start()
	{
		Searchtext = GetComponent<InputField>();
	}
	void Update()
	{
		if (Searchtext.isFocused)
		{
			InputFocus();
		}
		else if (!Searchtext.isFocused)
		{
			InputUnfocus();
		}
	}

	private void InputFocus()
	{
		//disable keyboard commands while input is focused
		UIManager.IsInputFocus = true;
	}
	private void InputUnfocus()
	{
		//disable keyboard commands while input is focused
		UIManager.IsInputFocus = false;
	}
}
