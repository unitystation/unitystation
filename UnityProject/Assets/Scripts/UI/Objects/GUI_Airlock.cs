using UnityEngine;
using UnityEngine.UI;
using UI.Core.NetUI;
using Doors;
using Doors.Modules;

namespace UI.Objects
{
	public class GUI_Airlock : NetTab
	{
		[SerializeField] private NetText_label labelOpen = null;
		[SerializeField] private NetText_label labelBolts = null;
		[SerializeField] private NetText_label labelSafety = null;
		[SerializeField] private NetColorChanger safetyImageColor = default;
		[SerializeField] private Color safetyImageColorWhenSAFE;
		[SerializeField] private Color safetyImageColorWhenHARM;
		[SerializeField] private Color safetyImageColorWhenNOPOWER;

		private DoorMasterController doorMasterController;
		private DoorMasterController master => doorMasterController ??= Provider.GetComponent<DoorMasterController>();
		private GameObject performer;

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			performer = connectedPlayer.GameObject;

			master.UpdateGUIEvent += OnUpdateGUIEvent;

			OnUpdateGUIEvent();
		}

		private void OnUpdateGUIEvent()
		{
			labelOpen.MasterSetValue(master.IsClosed ? "Closed" : "Open");

			if (master.Bolts != null)
				labelBolts.MasterSetValue(master.Bolts.BoltsDown ? "Bolted" : "Unbolted");
			else
				labelBolts.MasterSetValue("No Bolt Module");

			if (master.ElectrifyModule != null && master.HasPower)
			{
				if (master.ElectrifyModule.IsElectrified || master.HackingProcessBase.HasConnection(PreventElectrocution) == false)
				{
					labelSafety.MasterSetValue("DANGER");
					safetyImageColor.MasterSetValue(safetyImageColorWhenHARM);
				}
				else
				{
					labelSafety.MasterSetValue("SAFE");
					safetyImageColor.MasterSetValue(safetyImageColorWhenSAFE);
				}
			}
			else
			{
				if (master.HasPower)
					labelSafety.MasterSetValue("No Safety Module");
				else
					labelSafety.MasterSetValue("No Power");

				safetyImageColor.MasterSetValue(safetyImageColorWhenNOPOWER);
			}
		}

		public void OnToggleAirLockSafety()
		{
			master.ToggleSafetyDoor(performer);
		}

		public void OnToggleOpenDoor()
		{
			master.ToggleOpenDoor(performer);
		}

		public void OnToggleBoltDoor()
		{
			master.ToggleBoltDoor(performer);
		}

		public void OnTabClosedHandler()
		{
			master.UpdateGUIEvent -= OnUpdateGUIEvent;
		}
	}
}
