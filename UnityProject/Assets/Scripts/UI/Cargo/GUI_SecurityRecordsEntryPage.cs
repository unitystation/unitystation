using System.Collections;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public class GUI_SecurityRecordsEntryPage : NetPage
{
	private SecurityRecord record;
	private GUI_SecurityRecords securityRecordsTab;
	[SerializeField]
	private NetLabel nameText;
	[SerializeField]
	private NetLabel idText;
	[SerializeField]
	private NetLabel sexText;
	[SerializeField]
	private NetLabel ageText;
	[SerializeField]
	private NetLabel speciesText;
	[SerializeField]
	private NetLabel rankText;
	[SerializeField]
	private NetLabel fingerprintText;
	[SerializeField]
	private EmptyItemList crimesList = null;

	public void OnOpen(SecurityRecord recordToOpen, GUI_SecurityRecords recordsTab)
	{
		record = recordToOpen;
		securityRecordsTab = recordsTab;
		UpdateEntry();
	}

	private void UpdateEntry()
	{
		if (!CustomNetworkManager.Instance._isServer)
			return;
		if (record == null)
			return;
		nameText.SetValue = record.EntryName;
		idText.SetValue = record.ID;
		sexText.SetValue = record.Sex;
		ageText.SetValue = record.Age;
		speciesText.SetValue = record.Species;
		rankText.SetValue = record.Rank;
		fingerprintText.SetValue = record.Fingerprints;
		UpdateCrimesList();
	}

	public void DeleteCrime(SecurityRecordCrime crimeToDelete)
	{
		record.Crimes.Remove(crimeToDelete);
		UpdateEntry();
	}

	public void DeleteRecord()
	{
		SecurityRecordsManager.Instance.SecurityRecords.Remove(record);
		securityRecordsTab.OpenRecords();
	}

	private void UpdateCrimesList()
	{
		List<SecurityRecordCrime> crimes = record.Crimes;

		crimesList.Clear();
		crimesList.AddItems(crimes.Count);
		for (int i = 0; i < crimes.Count; i++)
		{
			GUI_SecurityRecordsCrime crimeItem = crimesList.Entries[i] as GUI_SecurityRecordsCrime;
			crimeItem.ReInit(crimes[i], this);
		}
	}
}