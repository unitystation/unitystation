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
	[SerializeField]
	private NetLabel statusButtonText;
	[SerializeField]
	private NetLabel idNameText;

	public void OnOpen(SecurityRecord recordToOpen, GUI_SecurityRecords recordsTab)
	{
		record = recordToOpen;
		securityRecordsTab = recordsTab;
		UpdateEntry();
	}

	public void IdNameUpdate()
	{
		if (securityRecordsTab == null)
			return;

		IDCard id = securityRecordsTab.InsertedCard;
		string str;

		if (id != null)
			str = $"{id.RegisteredName}, {id.GetJobType.ToString()}";
		else
			str = "********";
		idNameText.SetValue = str;
	}

	public void RemoveID()
	{
		securityRecordsTab.RemoveId();
		IdNameUpdate();
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
		statusButtonText.SetValue = record.Status.ToString();
		IdNameUpdate();
		UpdateCrimesList();
	}

	public void ChangeStatus()
	{
		switch (record.Status)
		{
			case SecurityStatus.None:
				record.Status = SecurityStatus.Arrest;
				break;
			case SecurityStatus.Arrest:
				record.Status = SecurityStatus.Parole;
				break;
			case SecurityStatus.Parole:
				record.Status = SecurityStatus.None;
				break;
		}
		statusButtonText.SetValue = record.Status.ToString();
	}

	public void NewCrime()
	{
		record.Crimes.Add(new SecurityRecordCrime());
		UpdateEntry();
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