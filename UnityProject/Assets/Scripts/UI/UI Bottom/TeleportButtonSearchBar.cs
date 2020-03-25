using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeleportButtonSearchBar : MonoBehaviour
{
	private InputField Searchtext;

	private List<GameObject> HiddenButtons = new List<GameObject>();

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

	public void Search()
	{
		foreach (GameObject x in HiddenButtons)
		{
			if (x != null)
			{
				x.SetActive(true);
			}
		}
		HiddenButtons.Clear();
		
		var buttons = gameObject.transform.parent.GetComponent<TeleportButtonControl>().teleportButtons;

		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i] != null)
			{
				if (buttons[i].GetComponent<TeleportButton>().myText.text.ToLower().Contains(Searchtext.text.ToLower()) | Searchtext.text.Length == 0)
				{
				}
				else
				{
					HiddenButtons.Add(buttons[i]);
					buttons[i].SetActive(false);
				}
			}
		}
	}

	public void Resettext()
	{
		Searchtext.text = "";
	}
}
