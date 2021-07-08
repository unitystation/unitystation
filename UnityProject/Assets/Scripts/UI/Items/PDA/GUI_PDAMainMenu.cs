using System;
using UnityEngine;

namespace UI.Items.PDA
{
	public class GUI_PDAMainMenu : NetPage, IPageReadyable
	{
		[SerializeField] private GUI_PDA controller = null;
		[SerializeField] private NetLabel idLabel = null;
		[SerializeField] private NetLabel lightLabel = null;

		private IDCard IDCard => controller.PDA.IDCard;

		private void Start()
		{
			controller.PDA.idSlotUpdated += UpdateIDStatus;
		}

		public void OnPageActivated()
		{
			UpdateElements();
		}

		public void EjectID()
		{
			controller.PDA.EjectIDCard();
			UpdateIDStatus();
			UpdateBreadcrumb();
		}

		public void ToggleFlashlight()
		{
			controller.PDA.ToggleFlashlight();
			UpdateFlashlightText();
		}

		private void UpdateElements()
		{
			UpdateBreadcrumb();
			UpdateIDStatus();
			UpdateFlashlightText();
		}

		private void UpdateBreadcrumb()
		{
			//Checks to see if the PDA has a registered name, if it does make that the Desktop name
			if (controller.PDA.RegisteredPlayerName != null)
			{
				string editedString = controller.PDA.RegisteredPlayerName.Replace(" ", "_");
				controller.SetBreadcrumb($"/home/{editedString}/Desktop");
			}
			else
			{
				controller.SetBreadcrumb("/home/Guest/Desktop");
			}
		}

		private void UpdateIDStatus()
		{
			if (IDCard != null)
			{
				SetIDStatus($"{IDCard.RegisteredName}, {IDCard.JobType}");
			}
			else
			{
				SetIDStatus("<No ID Inserted>");
			}

			if (controller.mainSwitcher.CurrentPage == this)
			{
				UpdateBreadcrumb();
			}
		}

		private void UpdateFlashlightText()
		{
			lightLabel.SetValueServer(controller.PDA.FlashlightOn ? "Flashlight (ON)" : "Flashlight (OFF)");
		}

		private void SetIDStatus(string status)
		{
			idLabel.SetValueServer(status);
		}
	}
}
