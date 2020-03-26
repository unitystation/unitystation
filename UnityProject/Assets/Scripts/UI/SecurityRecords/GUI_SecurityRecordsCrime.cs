﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_SecurityRecordsCrime : DynamicEntry
{
	private SecurityRecordCrime crime;
	private GUI_SecurityRecordsEntryPage entryPage;
	[SerializeField]
	private NetLabel crimeText = null;
	[SerializeField]
	private NetLabel detailsText = null;
	[SerializeField]
	private NetLabel authorText = null;
	[SerializeField]
	private NetLabel timeText = null;

	public void ReInit(SecurityRecordCrime crimeToInit, GUI_SecurityRecordsEntryPage entryPageToInit)
	{
		crime = crimeToInit;
		entryPage = entryPageToInit;

		crimeText.SetValue = crime.Crime;
		detailsText.SetValue = crime.Details;
		authorText.SetValue = crime.Author;
		timeText.SetValue = crime.Time;
	}

	public void DeleteCrime()
	{
		entryPage.DeleteCrime(crime);
	}

	public void SetEditingField(NetLabel fieldToEdit)
	{
		entryPage.SetEditingField(fieldToEdit, crime);
	}

	public void OpenPopup(NetLabel fieldToEdit)
	{
		//Previously we set entryPage only server-side, but popup is opening client-side
		if (entryPage == null)
			entryPage = GetComponentInParent<GUI_SecurityRecordsEntryPage>();
		entryPage.OpenPopup(fieldToEdit);
	}
}

[System.Serializable]
public class SecurityRecordCrime
{
	public string Crime;
	public string Details;
	public string Author;
	public string Time;

	public SecurityRecordCrime()
	{
		Crime = "None";
		Details = "-";
		Author = "The law";
		Time = "12:00";
	}

}
