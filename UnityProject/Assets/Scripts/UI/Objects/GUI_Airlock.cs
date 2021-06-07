using Doors;
using Doors.Modules;
using UnityEngine;

namespace UI.Objects
{
	public class GUI_Airlock : NetTab
	{
		[SerializeField]
		private NetLabel labelOpen = null;

		[SerializeField]
		private NetLabel labelBolts = null;

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
					bolts.SetBoltsState(!bolts.BoltsDown);;
					return;
				}
			}
		}
	}
}
