using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdminSearchBar : MonoBehaviour
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

	public InputField SearchText()
	{
		return Searchtext;
	}

	public void Resettext()//resets search field text everytime window is closed
	{
		Searchtext.text = "";
	}
}
