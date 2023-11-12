using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HubNotConnectedPopUp : MonoBehaviour
{
	public static HubNotConnectedPopUp Instance;

	public TMP_Text Text;
	public string ToPutOnClipboard;

	public Button ClipboardButton;

	public void Start()
	{
		Instance = this;
		Close();
	}

	public void SetUp(string OnFailText, string ClipboardURL)
	{
		gameObject.SetActive(true);
		Text.text = OnFailText;
		if (string.IsNullOrEmpty(ClipboardURL) == false)
		{
			ToPutOnClipboard = ClipboardURL;
			ClipboardButton.gameObject.SetActive(true);
		}
		else
		{
			ClipboardButton.gameObject.SetActive(false);
		}

	}

	public void SetClipBoard()
	{
		GUIUtility.systemCopyBuffer = ToPutOnClipboard;
	}

	public void Close()
	{
		this.gameObject.SetActive(false);
	}
}
