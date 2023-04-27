using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;
using Doors;
using Doors.Modules;

namespace UI.Objects
{
	public class GUI_Airlock : NetTab
	{
		[SerializeField]
		private NetText_label labelOpen = null;

		[SerializeField]
		private NetText_label labelBolts = null;

		[SerializeField] private NetText_label labelSafety = null;

		[SerializeField] private Image safetyImage;
		[SerializeField] private Color safetyImageColorWhenSAFE;
		[SerializeField] private Color safetyImageColorWhenHARM;
		[SerializeField] private Color safetyImageColorWhenNOPOWER;

		private DoorMasterController doorMasterController;
		private DoorMasterController DoorMasterController => doorMasterController ??= Provider.GetComponent<DoorMasterController>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			bool foundBolts = false;
			labelOpen.MasterSetValue( DoorMasterController.IsClosed ? "Closed" : "Open");

			foreach (var module in DoorMasterController.ModulesList)
			{
				if (module is BoltsModule bolts)
				{
					labelBolts.MasterSetValue(bolts.BoltsDown ? "Bolted" : "Unbolted");
					foundBolts = true;
				}
				if (module is ElectrifiedDoorModule electric)
				{
					labelSafety.MasterSetValue(electric.IsElectrified ? "DANGER" : "SAFE");
					UpdateSafetyStatusUI(electric);
				}
			}

			if (foundBolts == false)
			{
				labelBolts.MasterSetValue("No Bolt Module");
			}
		}

		public void OnToggleAirLockSafety()
		{
			if (DoorMasterController.CanAIInteract() == false) return;

			foreach (var module in DoorMasterController.ModulesList)
			{
				if (module is ElectrifiedDoorModule electric)
				{
					electric.ToggleElectrocutionInput();
					doorMasterController.UpdateGui();
					UpdateSafetyStatusUI(electric);
					break;
				}
			}
		}

		private void UpdateSafetyStatusUI(ElectrifiedDoorModule door)
		{
			//(Max): This is broken for some reason and doesn't work.
			if (DoorMasterController.CanAIInteract() == false) return;

			if (doorMasterController.HasPower == false)
			{
				safetyImage.color = safetyImageColorWhenNOPOWER;
				return;
			}

			safetyImage.color = door.IsElectrified ? safetyImageColorWhenHARM : safetyImageColorWhenSAFE;
		}

		public void OnToggleOpenDoor()
		{
			if (DoorMasterController.HasPower == false) return;
			if (DoorMasterController.CanAIInteract() == false) return;

			if (DoorMasterController.IsClosed)
            {
	            DoorMasterController.TryForceOpen();
            }
            else
            {
	            DoorMasterController.PulseTryForceClose();
            }
		}

		public void OnToggleBoltDoor()
		{
			if (DoorMasterController.HasPower == false) return;

			if (DoorMasterController.CanAIInteract() == false) return;

			foreach (var module in DoorMasterController.ModulesList)
			{
				if (module is BoltsModule bolts)
				{
					//Toggle bolts
					bolts.PulseToggleBolts();
					return;
				}
			}
		}
	}
}
