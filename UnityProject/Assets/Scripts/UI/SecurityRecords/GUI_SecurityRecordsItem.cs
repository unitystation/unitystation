using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SecurityRecords item is used inside EntriesPage.
/// </summary>
public class GUI_SecurityRecordsItem : DynamicEntry
{
	private GUI_SecurityRecords securityRecordsTab;
	private SecurityRecord securityRecord;
	[SerializeField]
	private NetLabel recordNameText = null;
	[SerializeField]
	private NetLabel recordIdText = null;
	[SerializeField]
	private NetLabel recordRankText = null;
	[SerializeField]
	private NetLabel recordFingerprintsText = null;
	[SerializeField]
	private NetLabel recordStatusText = null;
	[SerializeField]
	private NetColorChanger recordBgColor = null;

	public void ReInit(SecurityRecord record, GUI_SecurityRecords recordsTab)
	{
		if (record == null)
		{
			Logger.Log("SecurityRecordItem: no record found, not doing init", Category.NetUI);
			return;
		}
		securityRecord = record;
		securityRecordsTab = recordsTab;
		recordNameText.SetValue = record.EntryName;
		recordIdText.SetValue = record.ID;
		recordRankText.SetValue = record.Rank;
		recordFingerprintsText.SetValue = record.Fingerprints;
		recordStatusText.SetValue = record.Status.ToString();
		recordBgColor.SetValue = GetStatusColor(record.Status);
	}

	private string GetStatusColor(SecurityStatus status)
	{
		switch (status)
		{
			case SecurityStatus.None:
				return "424142";
			case SecurityStatus.Arrest:
				return "C10000";
			case SecurityStatus.Parole:
				return "F57211";
		}
		return ("424142");
	}

	public void OpenRecord()
	{
		securityRecordsTab.OpenRecord(securityRecord);
	}
}
