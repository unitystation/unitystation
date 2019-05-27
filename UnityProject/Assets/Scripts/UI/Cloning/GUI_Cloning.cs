using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Cloning : NetTab
{
	public CloningConsole CloningConsole;

	void Start()
	{
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			CloningConsole = Provider.GetComponentInChildren<CloningConsole>();
			//Subscribe to change event from CloningConsole.cs
			CloningConsole.changeEvent += UpdateDisplay;
			UpdateDisplay();
		}
	}


	public void UpdateDisplay()
	{

	}

	public void OnDestroy()
	{
		//Unsubscribe container update event
		CloningConsole.changeEvent -= UpdateDisplay;
	}

	public void StartScan()
	{
		UpdateDisplay();
	}

	public void LockScanner()
	{
		UpdateDisplay();
	}

	public void DeleteRecord()
	{
		UpdateDisplay();
	}

	public void Clone()
	{
		UpdateDisplay();
	}


}