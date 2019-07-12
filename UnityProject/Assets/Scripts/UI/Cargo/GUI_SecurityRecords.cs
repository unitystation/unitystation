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
	[SerializeField]
	private GUI_SecurityRecordsEntriesPage entriesPage;
	[SerializeField]
	private GUI_SecurityRecordsEntryPage entryPage;
	[SerializeField]
	private NetLabel idText;
	private SecurityRecordsConsole console;
	public IDCard InsertedCard;

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

		if (CustomNetworkManager.Instance._isServer)
		{
			console = Provider.GetComponentInChildren<SecurityRecordsConsole>();
			console.OnConsoleUpdate.AddListener(UpdateScreen);
		}
	}

	public void UpdateScreen()
	{
		if (InsertedCard == null && console.IdCard != null)
			InsertId(console.IdCard);

		if (nestedSwitcher.CurrentPage == entriesPage)
			entriesPage.IdNameUpdate();
		else if (nestedSwitcher.CurrentPage == entryPage)
			entryPage.IdNameUpdate();
		else
			UpdateIdText();
	}

	/// <summary>
	/// Insert some ID into console and update login details.
	/// Will spit out currently inserted ID card.
	/// </summary>
	///<param name="cardToInsert">Card you want to insert</param>
	private void InsertId(IDCard cardToInsert)
	{
		if (InsertedCard != null)
			RemoveId();
		InsertedCard = cardToInsert;
	}

	/// <summary>
	/// Spits out ID card from console and updates login details.
	/// </summary>
	public void RemoveId()
	{
		if (console != null && InsertedCard == null && console.IdCard != null)
			InsertedCard = console.IdCard;
		if (InsertedCard == null)
			return;
		ObjectBehaviour objBeh = InsertedCard.GetComponentInChildren<ObjectBehaviour>();

		Vector3Int pos = console.gameObject.WorldPosServer().RoundToInt();
		CustomNetTransform netTransform = objBeh.GetComponent<CustomNetTransform>();
		netTransform.AppearAtPosition(pos);
		netTransform.AppearAtPositionServer(pos);
		console.IdCard = null;
		InsertedCard = null;
		UpdateIdText();
	}

	private void UpdateIdText()
	{
		if (InsertedCard != null)
			idText.SetValue = $"{InsertedCard.RegisteredName}, {InsertedCard.GetJobType.ToString()}";
		else
			idText.SetValue = "********";
	}

	public void LogIn()
	{
		if (console != null && InsertedCard == null && console.IdCard != null)
			InsertedCard = console.IdCard;
		if (InsertedCard == null ||
			!InsertedCard.accessSyncList.Contains((int)Access.security))
			return;
		OpenRecords();
	}

	public void LogOut()
	{
		nestedSwitcher.SetActivePage(nestedSwitcher.DefaultPage);
		UpdateIdText();
	}

	public void OpenRecords()
	{
		nestedSwitcher.SetActivePage(entriesPage);
		entriesPage.OnOpen(this);
	}

	public void OpenRecord(SecurityRecord recordToOpen)
	{
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
	//This is needed for photo as I didn't came up with nicer solution
	public PlayerScript player;

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