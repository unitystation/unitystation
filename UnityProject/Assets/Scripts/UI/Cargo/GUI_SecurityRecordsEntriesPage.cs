using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_SecurityRecordsEntriesPage : NetPage
{
	[SerializeField]
	private EmptyItemList recordsList = null;
	private GUI_SecurityRecords securityRecordsTab;

	public void OnOpen(GUI_SecurityRecords recordsTab)
	{
		securityRecordsTab = recordsTab;
	}

	public void UpdateTab()
	{
		List<SecurityRecord> records = SecurityRecordsManager.Instance.SecurityRecords;

		recordsList.Clear();
		recordsList.AddItems(records.Count);
		for (int i = 0; i < records.Count; i++)
		{
			GUI_SecurityRecordsItem item = recordsList.Entries[i] as GUI_SecurityRecordsItem;
			item.ReInit(records[i], securityRecordsTab);
		}
	}

	public void NewRecord()
	{
		List<SecurityRecord> records = SecurityRecordsManager.Instance.SecurityRecords;
		SecurityRecord record = new SecurityRecord();

		record.EntryName = "New Record";
		record.ID = "-";
		record.Fingerprints = "-";
		record.Status = SecurityStatus.None;
		records.Add(record);
	}
}
