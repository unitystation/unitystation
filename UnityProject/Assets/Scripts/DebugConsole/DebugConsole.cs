using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
	private static DebugConsole debugConsole;
	public static DebugConsole Instance
	{
		get
		{
			if (debugConsole == null)
			{
				debugConsole = FindObjectOfType<DebugConsole> ();
			}
			return debugConsole;
		}
	}

	protected static string DebugLog { get; private set; }
	protected static string LastLog { get; private set; }

	public Text displayText;
	public GameObject consoleObject;

	bool isOpened = false;

	public static void AmendLog (string msg)
	{
		DebugLog += msg + "\n";
		LastLog = msg;
		Instance.RefreshLogDisplay();
	}

	void Start ()
	{
		Instance.consoleObject.SetActive (false);
		Instance.isOpened = false;
	}

	void Update ()
	{
		if (Input.GetKeyDown(KeyCode.F5))
		{
			ToggleConsole ();
		}
	}

	void ToggleConsole ()
	{
		isOpened = !isOpened;
		consoleObject.SetActive(isOpened);
	}

	void RefreshLogDisplay(){
		Instance.displayText.text = DebugLog;
	}

}