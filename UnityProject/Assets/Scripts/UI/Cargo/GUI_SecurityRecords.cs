using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_SecurityRecords : NetTab
{
	[SerializeField]
	private NetPageSwitcher nestedSwitcher;
	public IDCard InsertedCard;
	[SerializeField]
	private GUI_SecurityRecordsEntriesPage entriesPage;
	[SerializeField]
	private GUI_SecurityRecordsEntryPage entryPage;

	protected override void InitServer()
	{
		foreach (NetPage page in nestedSwitcher.Pages)
		{
			//page.GetComponent<GUI_CargoPage>().Init();
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
	}

	/// <summary>
	/// Insert some ID into console.
	/// Returns ID from console if there is one.
	/// </summary>
	/// <param name="cardToInsert">card to insert into console</param>
	/// <returns>Returns ID card if it were there before</returns>
	public IDCard InsertID()
	{
		IDCard cardToInsert = null;
		Debug.Log(Provider.name + "PROVIDER");
		//

		IDCard cardToReturn = null;
		if (InsertedCard != null)
			cardToReturn = InsertedCard;
		InsertedCard = cardToInsert;
		return (cardToReturn);
	}

	public void RemoveID()
	{
		//Add InsertedCard to player's hand
		InsertedCard = null;
	}

	public void LogIn()
	{
		InsertID();
		if (InsertedCard == null ||
			!InsertedCard.accessSyncList.Contains((int)Access.security))
			Debug.Log("No access");
		OpenRecords();
		//nestedSwitcher.SetActivePage(entriesPage);
		//entriesPage.GetComponent<GUI_SecurityRecordsEntriesPage>().UpdateTab();
	}

	public void LogOut()
	{
		nestedSwitcher.SetActivePage(nestedSwitcher.DefaultPage);
	}

	public void OpenRecords()
	{
		nestedSwitcher.SetActivePage(entriesPage);
		entriesPage.OnOpen(this);
	}

	public void OpenRecord(SecurityRecord recordToOpen)
	{
		Debug.Log("Opening shit");
		nestedSwitcher.SetActivePage(entryPage);
		entryPage.OnOpen(recordToOpen, this);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}

public enum SecurityStatus
{
	None,
	Arrest,
	Parole
}

[System.Serializable]
public class SecurityRecord
{
	public string EntryName;
	public string ID;
	public string Sex;
	public string Age;
	public string Species;
	public string Rank;
	public string Fingerprints;
	public SecurityStatus Status;
	public List<SecurityRecordCrime> Crimes;

	public SecurityRecord()
	{
		EntryName = "NewEntry";
		ID = "-";
		Sex = "-";
		Age = "99";
		Species = "Human";
		Rank = "Visitor";
		Fingerprints = "-";
		Status = SecurityStatus.None;
		Crimes = new List<SecurityRecordCrime>();
	}
}