using System;
using UnityEngine;

public class GUI_PDASettingMenu : NetPage
{
	[SerializeField] private GUI_PDA controller;

	[SerializeField] public NetLabel reset;

	private bool selectionCheck; // always set to false unless told otherwise

	// It sends you back, what did you expect?
	public void Back()
	{
		controller.OpenMainMenu();
	}

	//Logic pushed to controller for safety checks, cant have client fucking shit up
	public void SetNotificationSound(string notificationString)
	{
		if (controller.TestForUplink(notificationString) != true)
		{
			Debug.LogError("Sounds not implimented");
		}
	}
	// Makes the PDA  look like it just spawns and tells PDA class to make itself "Unknown" on messenger
	public void FactoryReset()
	{
		if (selectionCheck)
		{
			selectionCheck = false;
			reset.Value = "Factory Reset";
			controller.ResetPda();

		}
		else
		{
			selectionCheck = true;
			reset.Value = "Click again to confirm factory reset";
		}
	}
	// Supposed to handle the changing of UI themes, might drop this one
	public void Themes()
	{
		Debug.LogError("UI themes are not implimented yet!");
	}
}