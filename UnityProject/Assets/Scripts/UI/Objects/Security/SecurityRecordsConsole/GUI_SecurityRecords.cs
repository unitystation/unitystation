using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GUI_SecurityRecords : NetTab
{
	[SerializeField]
	private NetPageSwitcher nestedSwitcher = null;
	[SerializeField]
	private GUI_SecurityRecordsEntriesPage entriesPage = null;
	[SerializeField]
	private GUI_SecurityRecordsEntryPage entryPage = null;
	[SerializeField]
	private NetLabel idText = null;
	private SecurityRecordsConsole console;

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
		}
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		console = Provider.GetComponentInChildren<SecurityRecordsConsole>();
		console.OnConsoleUpdate.AddListener(UpdateScreen);
		UpdateScreen();
	}

	public void UpdateScreen()
	{
		if (nestedSwitcher.CurrentPage == entriesPage)
		{
			entriesPage.OnOpen(this);
		}
		else if (nestedSwitcher.CurrentPage == entryPage)
		{
			entryPage.UpdateEntry();
		}
		else
		{
			UpdateIdText(idText);
		}
	}

	public void RemoveId()
	{
		if (console.IdCard)
		{
			console.ServerRemoveIDCard();
			UpdateScreen();
		}
	}

	public void UpdateIdText(NetLabel labelToSet)
	{
		var IdCard = console.IdCard;
		if (IdCard)
		{
			labelToSet.SetValueServer($"{IdCard.RegisteredName}, {IdCard.JobType.ToString()}");
		}
		else
		{
			labelToSet.SetValueServer("********");
		}
	}

	public void LogIn()
	{
		if (console.IdCard == null || !console.IdCard.HasAccess(Access.security))
		{
			return;
		}

		OpenRecords();
	}

	public void LogOut()
	{
		nestedSwitcher.SetActivePage(nestedSwitcher.DefaultPage);
		UpdateIdText(idText);
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
	public Occupation Occupation;
	public CharacterSettings characterSettings;

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
