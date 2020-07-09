using System;
using UnityEngine;

namespace UI.PDA
{
	public class GUI_PDAMainMenu : NetPage
	{
		//references to essential MainMenu elements
		[SerializeField] private GUI_PDA controller;

		[SerializeField] private NetLabel idLabel;

		[SerializeField] private NetLabel lightLabel;

		[SerializeField] private NetLabel machineLabel;

		//These keep the current text cached for text refresh purposes
		private string cachedidText;
		private string cachedMachineLabelText;
		private string cachedFalshlightText;


		/// <summary>
		/// This runs to make sure that the code is actually working, doesnt do much else
		/// </summary>
		private void Start()
		{
			idLabel.SetValueServer("<No ID Inserted>");
			machineLabel.SetValueServer("/home/Guest/Desktop");
			lightLabel.SetValueServer("Flashlight (ON)");
			ToggleFlashLight();
		}
		
		public void RefreshText()
		{
			if (gameObject.activeInHierarchy != true) return;
			idLabel.SetValueServer(cachedidText);
			machineLabel.SetValueServer(cachedMachineLabelText);
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
				cachedidText = $"{idCard.RegisteredName},{idCard.JobType}";
				idLabel.SetValueServer(cachedidText);
			}
			else
			{
				cachedidText = "<No ID Inserted>";
				idLabel.SetValueServer(cachedidText);
			}
			//Checks to see if the PDA has a registered name, if it does make that the Desktop name
			if (controller.Pda.PdaRegisteredName != null)
			{
				string editedString = controller.Pda.PdaRegisteredName.Replace(" ","_");
				cachedMachineLabelText = $"/home/{editedString}/Desktop";
				machineLabel.SetValueServer(cachedMachineLabelText);
			}
			else
			{
				cachedMachineLabelText = "/home/Guest/Desktop";
				machineLabel.SetValueServer(cachedMachineLabelText);
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
			lightLabel.SetValueServer(controller.Pda.FlashlightOn ? "Flashlight (ON)" : "Flashlight (OFF)");
		}
	}
}