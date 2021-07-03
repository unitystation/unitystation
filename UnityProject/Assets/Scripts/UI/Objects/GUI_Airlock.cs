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

		[SerializeField] private Image safetyImage = null;
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
			labelOpen.Value = DoorMasterController.IsClosed ? "Closed" : "Open";
			labelSafety.Value = DoorMasterController.IsElectrecuted ? "DANGER" : "SAFE";
			UpdateSafetyStatusUI();

			foreach (var module in DoorMasterController.ModulesList)
			{
				if(module is BoltsModule bolts)
				{

					labelBolts.Value = bolts.BoltsDown ? "Bolted" : "Unbolted";
					return;
				}
			}

			labelBolts.Value = "No Bolt Module";
		}

		public void OnToggleAirLockSafety()
		{
			if (DoorMasterController.HasPower == false) return;

			doorMasterController.IsElectrecuted = !doorMasterController.IsElectrecuted;
			UpdateSafetyStatusUI();
		}

		private void UpdateSafetyStatusUI()
		{
			labelSafety.Value = DoorMasterController.IsElectrecuted ? "DANGER" : "SAFE";
			if (doorMasterController.HasPower == false)
			{
				safetyImage.color = safetyImageColorWhenNOPOWER;
				return;
			}
			if (doorMasterController.IsElectrecuted)
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
