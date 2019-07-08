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
