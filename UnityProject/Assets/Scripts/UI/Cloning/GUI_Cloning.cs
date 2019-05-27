using System.Collections;
using System.Collections.Generic;
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
			//Subscribe to change event from CloningConsole.cs
			CloningConsole.changeEvent += UpdateDisplay;
			UpdateDisplay();
		}
	}

	public void UpdateDisplay()
	{
        DisplayAllRecords();
		DisplayCurrentRecord();
	}

	public void OnDestroy()
	{
		//Unsubscribe container update event
		CloningConsole.changeEvent -= UpdateDisplay;
	}

	public void StartScan()
	{
        Logger.Log("Scanning", Category.NetUI);
		UpdateDisplay();
	}

	public void LockScanner()
	{
        Logger.Log("Toggling scanner lock", Category.NetUI);
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
        CloningConsole.CloningRecords.Remove(specificRecord);
        specificRecord = null;
	}

	public void Clone()
	{
        Logger.Log($"Cloning {specificRecord.Name}", Category.NetUI);
        RemoveRecord();
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
			recordName.Value = specificRecord.Name;
			recordScanID.Value = "Scan ID " + specificRecord.ScanID;
			recordOxy.Value = specificRecord.OxyDmg + "\tOxygen Damage";
			recordBurn.Value = specificRecord.BurnDmg + "\tBurn Damage";
			recordToxin.Value = specificRecord.ToxingDmg + "\tToxin Damage";
			recordBrute.Value = specificRecord.BruteDmg + "\tBrute Damage";
			recordUniqueID.Value = specificRecord.UniqueIdentifier;
        }
	}

    public void DisplayAllRecords()
    {
        List<CloningRecord> cloningRecords = CloningConsole.CloningRecords;

        recordList.Clear();
        recordList.AddItems(cloningRecords.Count);

        for (int i = 0; i < cloningRecords.Count; i++)
        {
            GUI_CloningRecordItem item = recordList.Entries[i] as GUI_CloningRecordItem;
			item.gui_Cloning = this;
            item.cloningRecord = cloningRecords[i];
			item.SetValues();
            item.gameObject.SetActive(true);
        }
    }

}