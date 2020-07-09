using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDASettingMenu : NetPage
	{
		[SerializeField] private GUI_PDA controller;

		[SerializeField] public NetLabel input;

		private bool selectionCheck; // a simple variable to make sure the PDA asks the player to confirm the reset

		//Logic pushed to controller for safety checks, cant have client fucking shit up
		public void SetNotificationSound(string notificationString)
		{
			if (controller.TestForUplink(notificationString) != true)
			{
				Debug.LogError("Sounds not implimented");
			}
			input.SetValueServer("");
		}

		private void ResetTimer()
		{
			WaitFor.Seconds(1);
			selectionCheck = false;
		}

		/// <summary>
		/// Tells the PDA to unregister the name and tell the messenger that it is "unknown"
		/// </summary>
		public void FactoryReset()
		{
			if (selectionCheck)
			{
				selectionCheck = false;
				controller.ResetPda();

			}
			else
			{
				StartCoroutine("ResetTimer");
				selectionCheck = true;
			}
		}
		// Supposed to handle the changing of UI themes, might drop this one
		public void Themes()
		{
			Debug.LogError("UI themes are not implimented yet!");
		}
	}
}