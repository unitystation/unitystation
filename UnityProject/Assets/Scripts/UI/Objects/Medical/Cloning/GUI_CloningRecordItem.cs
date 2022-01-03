using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Objects.Medical;

namespace UI.Objects.Medical
{
	public class GUI_CloningRecordItem : DynamicEntry
	{
		public CloningRecord cloningRecord;
		public GUI_Cloning gui_Cloning;

		public NetLabel recordName;
		public NetLabel recrodScanID;

		public void SetValues()
		{
			recordName.SetValueServer(cloningRecord.name);
			recrodScanID.SetValueServer("Scan ID " + cloningRecord.scanID);
		}

		public void ViewRecord()
		{
			gui_Cloning.ViewRecord(cloningRecord);
		}
	}
}
