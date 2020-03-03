using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class GUI_Cloning : NetTab
{
	public CloningConsole CloningConsole;
	public GUI_CloningItemList recordList = null;

	public NetPageSwitcher netPageSwitcher;
	public NetPage PageAllRecords;
	public NetPage PageSpecificRecord;

	public CloningRecord specificRecord;

	public NetLabel[] cloningPodStatus;
	public NetLabel scannerStatus;
	public NetLabel buttonTextViewRecord;
	public NetLabel recordName;
	public NetLabel recordScanID;
	public NetLabel recordOxy;
	public NetLabel recordBurn;
	public NetLabel recordToxin;
	public NetLabel recordBrute;
	public NetLabel recordUniqueID;

	void Start()
	{
		if (Provider != null)
		{
			//Makes sure it connects with the dispenser properly
			CloningConsole = Provider.GetComponentInChildren<CloningConsole>();
			CloningConsole.RegisterConsoleGUI(this);
			//Subscribe to change event from CloningConsole.cs
			CloningConsole.changeEvent += UpdateDisplay;
			UpdateDisplay();
		}
	}

	public void UpdateDisplay()
	{
		DisplayAllRecords();
		DisplayCurrentRecord();
		DisplayPodStatus();
		DisplayScannerStatus();
		buttonTextViewRecord.SetValue = $"View Records({CloningConsole.CloningRecords.Count()})";
	}

	public void OnDestroy()
	{
		//Unsubscribe container update event
		CloningConsole.changeEvent -= UpdateDisplay;
	}

	public void StartScan()
	{
		CloningConsole.Scan();
		UpdateDisplay();
	}

	public void LockScanner()
	{
		CloningConsole.ServerToggleLock();
		UpdateDisplay();
	}

	public void DeleteRecord()
	{
		RemoveRecord();
		UpdateDisplay();
		netPageSwitcher.SetActivePage(PageAllRecords);
	}

	public void RemoveRecord()
	{
		CloningConsole.RemoveRecord(specificRecord);
		specificRecord = null;
	}

	public void Clone()
	{
		CloningConsole.ServerTryClone(specificRecord);
		UpdateDisplay();
		netPageSwitcher.SetActivePage(PageAllRecords);
	}

	public void ViewAllRecords()
	{
		UpdateDisplay();
		netPageSwitcher.SetActivePage(PageAllRecords);
	}

	public void ViewRecord(CloningRecord cloningRecord)
	{
		specificRecord = cloningRecord;
		UpdateDisplay();
		netPageSwitcher.SetActivePage(PageSpecificRecord);
	}

	public void DisplayCurrentRecord()
	{
		if(specificRecord != null)
		{
			recordName.SetValue = specificRecord.name;
			recordScanID.SetValue = "Scan ID " + specificRecord.scanID;
			recordOxy.SetValue = specificRecord.oxyDmg + "\tOxygen Damage";
			recordBurn.SetValue = specificRecord.burnDmg + "\tBurn Damage";
			recordToxin.SetValue = specificRecord.toxinDmg + "\tToxin Damage";
			recordBrute.SetValue = specificRecord.bruteDmg + "\tBrute Damage";
			recordUniqueID.SetValue = specificRecord.uniqueIdentifier;
		}
	}

	public void DisplayAllRecords()
	{
		recordList.Clear();
		recordList.AddItems(CloningConsole.CloningRecords.Count());

		var i = 0;
		foreach (var cloningRecord in CloningConsole.CloningRecords)
		{
			GUI_CloningRecordItem item = recordList.Entries[i] as GUI_CloningRecordItem;
			item.gui_Cloning = this;
			item.cloningRecord = cloningRecord;
			item.SetValues();
			i++;
		}
	}

	public void DisplayPodStatus()
	{
		string text;
		if(CloningConsole.CloningPod)
		{
			text = CloningConsole.CloningPod.statusString;
		}
		else
		{
			text = "ERROR: no pod detected.";
		}
		for (int i = 0; i < cloningPodStatus.Length; i++)
		{
			cloningPodStatus[i].SetValue = text;
		}
	}

	public void DisplayScannerStatus()
	{
		if(CloningConsole.Scanner)
		{
			scannerStatus.SetValue = CloningConsole.Scanner.statusString;
		}
		else
		{
			scannerStatus.SetValue = "ERROR: no DNA scanner detected.";
		}
	}

}