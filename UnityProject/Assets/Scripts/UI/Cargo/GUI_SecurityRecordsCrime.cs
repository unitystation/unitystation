using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_SecurityRecordsCrime : DynamicEntry
{
	private SecurityRecordCrime crime;
	private GUI_SecurityRecordsEntryPage entryPage;
	[SerializeField]
	private NetLabel crimeText;
	[SerializeField]
	private NetLabel detailsText;
	[SerializeField]
	private NetLabel authorText;
	[SerializeField]
	private NetLabel timeText;

	public void ReInit(SecurityRecordCrime crimeToInit, GUI_SecurityRecordsEntryPage entryPageToInit)
	{
		Debug.Log("page 1 " + entryPage);
		crime = crimeToInit;
		entryPage = entryPageToInit;
		Debug.Log("page 2 " + entryPage);

		crimeText.SetValue = crime.Crime;
		detailsText.SetValue = crime.Details;
		authorText.SetValue = crime.Author;
		timeText.SetValue = crime.Time;
	}

	public void DeleteCrime()
	{
		Debug.Log("page 3 " + entryPage);
		entryPage.DeleteCrime(crime);
	}

	public void SetEditingField(NetLabel fieldToEdit)
	{
		entryPage.SetEditingField(fieldToEdit, crime);
	}

	public void OpenPopup()
	{
		Debug.Log("page 4 " + entryPage);
		entryPage.OpenPopup();
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
