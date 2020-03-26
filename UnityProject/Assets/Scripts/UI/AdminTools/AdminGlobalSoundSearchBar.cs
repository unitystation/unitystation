using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// SearchBar on the window
/// </summary>
public class AdminGlobalSoundSearchBar : MonoBehaviour
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
		foreach (GameObject x in HiddenButtons)//Hidden Buttons stores list of the hidden items which dont contain the search phrase
		{
			if (x != null)
			{
				x.SetActive(true);
			}
		}
		HiddenButtons.Clear();

		var buttons = gameObject.transform.parent.GetComponent<AdminGlobalSound>().soundButtons;//Grabs fresh list of all the possible buttons as gameobjects

		for (int i = 0; i < buttons.Count; i++)
		{
			if (buttons[i] != null)
			{
				if (buttons[i].GetComponent<AdminGlobalSoundButton>().myText.text.ToLower().Contains(Searchtext.text.ToLower()) | Searchtext.text.Length == 0)
				{
				}
				else
				{
					HiddenButtons.Add(buttons[i]);//non-results get hidden
					buttons[i].SetActive(false);
				}
			}
		}
	}

	public void Resettext()//resets search field text everytime window is closed
	{
		Searchtext = GetComponent<InputField>();
		Searchtext.text = "";
	}
}
