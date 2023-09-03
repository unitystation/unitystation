using System.Collections;
using System.Collections.Generic;
using Logs;
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
			Loggy.LogError($"{nameof(TMP_InputField)} not found / assigned to {this}.");
			enabled = false;
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}
	void UpdateMe()
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
