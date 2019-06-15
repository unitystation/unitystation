using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI_CloningRecordItem : DynamicEntry
{
	public CloningRecord cloningRecord;
	public GUI_Cloning gui_Cloning;

	public NetLabel recordName;
	public NetLabel recrodScanID;

	public void SetValues()
	{
		recordName.SetValue = cloningRecord.Name;
		recrodScanID.SetValue = "Scan ID " + cloningRecord.ScanID;
	}

	public void ViewRecord()
	{
		gui_Cloning.ViewRecord(cloningRecord);
	}
}
