using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAMainMenu : NetPage
	{
		[SerializeField] private GUI_PDA controller;

		[SerializeField] private NetLabel idLabel;

		[SerializeField] private NetLabel lightLabel;

		[SerializeField] private NetLabel machineLabel;


		private void Start()
		{
			idLabel.Value = "<No ID Inserted>";
			machineLabel.Value = "/home/Guest/Desktop";
		}


		/// <summary>
		/// Updates the ID on the MainMenu
		/// </summary>
		public void UpdateId()
		{
			if (!controller.Pda) return;
			IDCard idCard = controller.Pda.IdCard;
			// Not "Optimized" by rider for readability
			//Checks to see if the ID card is not null, if so then display the owner of the ID and their job
			if (idCard != null)
			{
				idLabel.Value = $"{idCard.RegisteredName},{idCard.JobType}";
			}
			else
			{
				idLabel.Value = "<No ID Inserted>";
			}
			//Checks to see if the PDA has a registered name, if it does make that the Desktop name
			if (controller.Pda.PdaRegisteredName != null)
			{
				string editedString = controller.Pda.PdaRegisteredName.Replace(" ","_");

				machineLabel.Value = $"/home/{editedString}/Desktop";
			}
			else
			{
				machineLabel.Value = "/home/Guest/Desktop";
			}
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