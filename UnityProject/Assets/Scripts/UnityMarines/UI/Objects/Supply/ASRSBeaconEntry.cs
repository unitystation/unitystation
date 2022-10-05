using System.Collections;
using Items;
using UI.Core.NetUI;
using UnityEngine;

namespace Objects.TGMC
{

	public class ASRSBeaconEntry : DynamicEntry
	{
		[SerializeField]
		private NetText_label beaconNameLabel = null;
		[SerializeField]
		private NetText_label beaconPosLabel = null;
		[SerializeField]
		private NetColorChanger beaconColorChanger = null;

		[SerializeField]
		private Color[] colors = new Color[2];

		private ASRSBeacon beacon;
		private GUI_ASRSField teleporterMain;

		bool selected = false;
		Vector3 beaconPos;

		public void OnDisable()
		{
			if (teleporterMain == null) return;

			teleporterMain.setEntriesSelect -= setSelect;
		}

		public void SetValues(GUI_ASRSField teleportMain, ASRSBeacon aSRSBeacon)
		{
			teleporterMain = teleportMain;
		
			teleporterMain.setEntriesSelect += setSelect;
		
			beacon = aSRSBeacon;
	
			beaconNameLabel.SetValueServer(beacon.Name);

			beaconPos = beacon.CurrentBeaconPosition();
	
			beaconPosLabel.SetValueServer($"({beaconPos.x}, {beaconPos.y}, {beaconPos.z})");
		}

		private void setSelect(bool selected)
		{
			if (selected == this.selected) return;

			this.selected = selected;

			if(selected == true)
			{
				beaconColorChanger.SetValueServer(colors[1]);
				return;
			}

			beaconColorChanger.SetValueServer(colors[0]);
		}

	

		public void OnPressed()
		{
			if (beacon == null || teleporterMain == null) return;

			teleporterMain.OnEntryPressed(beacon);

			setSelect(true);
		}
	}
}
