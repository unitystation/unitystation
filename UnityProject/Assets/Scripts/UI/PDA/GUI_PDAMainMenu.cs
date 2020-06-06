using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAMainMenu : NetPage
	{
		/*[SerializeField]
	[Tooltip("CrewManifest here")]
	private GUI_PDA_CrewManifest manifestPage = null;  //The menuPage for reference*/

		[SerializeField] private GUI_PDA controller;

		[SerializeField] private NetLabel idLabel;

		[SerializeField] private NetLabel lightLabel;

		[SerializeField] private NetLabel machineLabel;

		private string pdaName;


		/// <summary>
		/// Updates the ID on the MainMenu
		/// </summary>
		public void UpdateId()
		{
			var tempName = controller.Pda.PdaRegisteredName;
		}
		public void SettingsPage()
		{
			controller.OpenSettings();
		}

		/// <summary>
		/// Tells the PDA to eject the ID and updates the menu
		/// </summary>
		public void IdRemove()
		{
			controller.RemoveId();
			UpdateId();
		}

		/// <summary>
		/// Toggles the flashlight
		/// </summary>
		public void ToggleFlashLight()
		{
			controller.Pda.ToggleFlashlight();
			// A condensed version of an if statement made by rider, basically it switches between off and on, pretty neato
			lightLabel.Value = controller.Pda.FlashlightOn ? "Flashlight (ON)" : "Flashlight (OFF)";
		}
	}
}