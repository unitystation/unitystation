using System.Linq;
using Doors;
using Doors.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Objects
{
	public class GUI_Airlock : NetTab
	{
		[SerializeField]
		private NetLabel labelOpen = null;

		[SerializeField]
		private NetLabel labelBolts = null;

		[SerializeField] private NetLabel labelSafety = null;

		[SerializeField] private Image safetyImage;
		[SerializeField] private Color safetyImageColorWhenSAFE;
		[SerializeField] private Color safetyImageColorWhenHARM;
		[SerializeField] private Color safetyImageColorWhenNOPOWER;

		private DoorMasterController doorMasterController;
		private DoorMasterController DoorMasterController {
			get {
				if (doorMasterController == null)
					doorMasterController = Provider.GetComponent<DoorMasterController>();

				return doorMasterController;
			}
		}

		public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
		{
			bool foundBolts = false;
			labelOpen.Value = DoorMasterController.IsClosed ? "Closed" : "Open";

			foreach (var module in DoorMasterController.ModulesList)
			{
				if(module is BoltsModule bolts)
				{
					labelBolts.Value = bolts.BoltsDown ? "Bolted" : "Unbolted";
					foundBolts = true;
				}
				if (module is ElectrifiedDoorModule electric)
				{
					labelSafety.Value = electric.IsElectrecuted ? "DANGER" : "SAFE";
					UpdateSafetyStatusUI(electric);
				}
			}

			if(!foundBolts) labelBolts.Value = "No Bolt Module";
		}

		public void OnToggleAirLockSafety()
		{
			foreach (var module in DoorMasterController.ModulesList)
			{
				if (module is ElectrifiedDoorModule electric)
				{
					if(doorMasterController.HasPower) electric.IsElectrecuted = !electric.IsElectrecuted;
					doorMasterController.UpdateGui();
					UpdateSafetyStatusUI(electric);
					break;
				}
			}
		}

		private void UpdateSafetyStatusUI(ElectrifiedDoorModule door)
		{
			if (doorMasterController.HasPower == false)
			{
				safetyImage.color = safetyImageColorWhenNOPOWER;
				return;
			}
			if (door.IsElectrecuted)
			{
				safetyImage.color = safetyImageColorWhenHARM;
			}
			else
			{
				safetyImage.color = safetyImageColorWhenSAFE;
			}
		}

		public void OnToggleOpenDoor()
		{
			if (DoorMasterController.HasPower == false) return;

			if (DoorMasterController.IsClosed)
            {
	            DoorMasterController.TryForceOpen();
            }
            else
            {
	            DoorMasterController.TryForceClose();
            }
		}

		public void OnToggleBoltDoor()
		{
			if (DoorMasterController.HasPower == false) return;

			foreach (var module in DoorMasterController.ModulesList)
			{
				if(module is BoltsModule bolts)
				{
					//Toggle bolts
					bolts.SetBoltsState(!bolts.BoltsDown);
					return;
				}
			}
		}
	}
}
