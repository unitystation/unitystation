using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// use to focus InputBoxes
/// </summary>
public class WindowFocus : MonoBehaviour
{
	public TMP_InputField Searchtext;
	private void Start()
	{
		if (Searchtext == null)
		{
			Searchtext = GetComponent<TMP_InputField>();
		}

		if (Searchtext == null)
		{
			Logger.LogError($"{nameof(TMP_InputField)} not found / assigned to {this}.");
			enabled = false;
		}
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
