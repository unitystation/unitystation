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

	public InputField SearchText()
	{
		return Searchtext;
	}

	public void Resettext()//resets search field text everytime window is closed
	{
		Searchtext.text = "";
	}
}
