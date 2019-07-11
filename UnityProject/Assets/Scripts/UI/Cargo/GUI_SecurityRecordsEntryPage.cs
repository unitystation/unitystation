using System;
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
	[SerializeField]
	private GameObject popupWindow;
	private NetLabel currentlyEditingField;
	private SecurityRecordCrime currentlyEditingCrime;

	public void OnOpen(SecurityRecord recordToOpen, GUI_SecurityRecords recordsTab)
	{
		record = recordToOpen;
		securityRecordsTab = recordsTab;
		UpdateEntry();
	}

	private void OnEnable()
	{
		ClosePopup();
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

	public void OpenPopup()
	{
		popupWindow.SetActive(true);
	}

	public void SetEditingField(NetLabel fieldToEdit)
	{
		currentlyEditingField = fieldToEdit;
	}

	public void SetEditingField(NetLabel fieldToEdit, SecurityRecordCrime crimeToEdit)
	{
		currentlyEditingField = fieldToEdit;
		currentlyEditingCrime = crimeToEdit;
	}

	public void ConfirmPopup(string value)
	{
		currentlyEditingField.SetValue = value;
		string nameBeforeIndex = currentlyEditingField.name.Split('~')[0];
		switch (nameBeforeIndex)
		{
			case "NameText":
				record.EntryName = value;
				break;
			case "IdText":
				record.ID = value;
				break;
			case "SexText":
				record.Sex = value;
				break;
			case "AgeText":
				record.Age = value;
				break;
			case "SpeciesText":
				record.Species = value;
				break;
			case "RankText":
				record.Rank = value;
				break;
			case "FingerprintText":
				record.Fingerprints = value;
				break;
			case "CrimeText":
				currentlyEditingCrime.Crime = value;
				break;
			case "DetailsText":
				currentlyEditingCrime.Details = value;
				break;
			case "AuthorText":
				currentlyEditingCrime.Author = value;
				break;
			case "TimeText":
				currentlyEditingCrime.Time = value;
				break;
		}
	}

	public void ClosePopup()
	{
		popupWindow.SetActive(false);
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