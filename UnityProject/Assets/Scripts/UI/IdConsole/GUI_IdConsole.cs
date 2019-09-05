using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_IdConsole : NetTab
{
	private IdConsole console;
	[SerializeField]
	private List<EmptyItemList> accessCategoriesList;
	[SerializeField]
	private EmptyItemList assignList;
	[SerializeField]
	private NetPageSwitcher pageSwitcher;
	[SerializeField]
	private NetPage loginPage;
	[SerializeField]
	private NetPage usercardPage;
	[SerializeField]
	private NetPage mainPage;
	[SerializeField]
	private NetLabel targetCardName;
	[SerializeField]
	private NetLabel accessCardName;
	[SerializeField]
	private NetLabel loginCardName;

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

		console = Provider.GetComponentInChildren<IdConsole>();
		console.OnConsoleUpdate.AddListener(UpdateScreen);
		UpdateScreen();
	}

	public void UpdateScreen()
	{
		if (pageSwitcher.CurrentPage == loginPage)
		{
			UpdateLoginCardName();
			LogIn();
		}
		if (pageSwitcher.CurrentPage == usercardPage && console.TargetCard != null)
		{
			pageSwitcher.SetActivePage(mainPage);
			if (assignList.Entries.Length == 0)
			{
				CreateAccessList();
				CreateAssignList();
			}
		}
		if (pageSwitcher.CurrentPage == mainPage)
		{
			UpdateAccessList();
			UpdateAssignList();
		}
		UpdateCardNames();
		//update contained access on target card
		//update displayed cardholder name
		//update displayed cardholder assignment
	}

	private void UpdateLoginCardName()
	{
		loginCardName.SetValue = console.AccessCard != null ?
			$"{console.AccessCard.RegisteredName}, {console.AccessCard.GetJobType.ToString()}" : "********";
	}

	private void UpdateCardNames()
	{
		if (console.AccessCard != null)
		{
			accessCardName.SetValue = $"{console.AccessCard.RegisteredName}, {console.AccessCard.GetJobType.ToString()}";
		}
		else
		{
			accessCardName.SetValue = "-";
		}

		if (console.TargetCard != null)
		{
			targetCardName.SetValue = $"{console.TargetCard.RegisteredName}, {console.TargetCard.GetJobType.ToString()}";
		}
		else
		{
			targetCardName.SetValue = "-";
		}
	}

	private void UpdateAssignment()
	{
		UpdateAccessList();
		UpdateAssignList();
		UpdateCardNames();
	}

	private void CreateAccessList()
	{
		for (int i = 0; i < IdConsoleManager.Instance.AccessCategories.Count; i++)
		{
			List<IdAccess> accessList = IdConsoleManager.Instance.AccessCategories[i].IdAccessList;
			accessCategoriesList[i].Clear();
			accessCategoriesList[i].AddItems(accessList.Count);
			for (int j = 0; j < accessList.Count; j++)
			{
				GUI_IdConsoleEntry entry;
				entry = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntry;
				entry.SetUpAccess(this, console.TargetCard, accessList[j], IdConsoleManager.Instance.AccessCategories[i]);
			}
		}
	}

	private void UpdateAccessList()
	{
		for (int i = 0; i < IdConsoleManager.Instance.AccessCategories.Count; i++)
		{
			List<IdAccess> accessList = IdConsoleManager.Instance.AccessCategories[i].IdAccessList;
			for (int j = 0; j < accessList.Count; j++)
			{
				GUI_IdConsoleEntry entry;
				entry = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntry;
				entry.CheckIsSet();
			}
		}
	}

	private void CreateAssignList()
	{
		assignList.Clear();
		assignList.AddItems(GameManager.Instance.Occupations.Count);
		int i = 0;
		GUI_IdConsoleEntry entry;
		//foreach(JobType jobType in System.Enum.GetValues(typeof(JobType)))
		foreach (GameObject occupationGo in GameManager.Instance.Occupations)
		{
			JobType jobType = occupationGo.GetComponent<OccupationRoster>().Type;
			entry = assignList.Entries[i] as GUI_IdConsoleEntry;
			entry.SetUpAssign(this, console.TargetCard, GameManager.Instance.GetOccupationOutfit(jobType));
			i++;
		}
	}

	private void UpdateAssignList()
	{
		GUI_IdConsoleEntry entry;
		for (int i = 0; i < GameManager.Instance.Occupations.Count; i++)
		{
			entry = assignList.Entries[i] as GUI_IdConsoleEntry;
			entry.CheckIsSet();
		}
	}

	public void ChangeName(string newName)
	{
		console.TargetCard.RegisteredName = newName;
		UpdateCardNames();
	}

	public void ModifyAccess(Access accessToModify)
	{
		if (console.TargetCard.accessSyncList.Contains((int) accessToModify))
		{
			console.TargetCard.accessSyncList.Remove((int)accessToModify);
		}
		else
		{
			console.TargetCard.accessSyncList.Add((int)accessToModify);
		}
	}

	public void ChangeAssignment(JobOutfit jobToSet)
	{
		console.TargetCard.accessSyncList.Clear();
		console.TargetCard.AddAccessList(jobToSet.allowedAccess);
		console.TargetCard.jobTypeInt = (int)jobToSet.jobType;
		UpdateAssignment();
	}

	public void RemoveTargetCard()
	{
		if (console.TargetCard == null)
		{
			return;
		}
		console.EjectCard(console.TargetCard);
		console.TargetCard = null;
		pageSwitcher.SetActivePage(usercardPage);
	}

	public void RemoveAccessCard()
	{
		if (console.AccessCard == null)
		{
			return;
		}
		console.EjectCard(console.AccessCard);
		console.AccessCard = null;
		UpdateCardNames();
		LogOut();
	}

	public void LogIn()
	{
		if (console.AccessCard != null &&
			console.AccessCard.accessSyncList.Contains((int)Access.hop))
		{
			console.LoggedIn = true;
			pageSwitcher.SetActivePage(usercardPage);
			UpdateScreen();
		}
		Debug.Log("No access");
		//No access to console
	}

	public void LogOut()
	{
		RemoveTargetCard();
		console.LoggedIn = false;
		pageSwitcher.SetActivePage(loginPage);
		UpdateLoginCardName();
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}
