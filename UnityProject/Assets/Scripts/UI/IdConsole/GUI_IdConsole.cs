using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	private int jobsCount;

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
			jobsCount = OccupationList.Instance.Occupations.Count() - IdConsoleManager.Instance.IgnoredJobs.Count;
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

	//This method is super slow - call it only if the list is empty
	private void CreateAccessList()
	{
		for (int i = 0; i < IdConsoleManager.Instance.AccessCategories.Count; i++)
		{
			List<IdAccess> accessList = IdConsoleManager.Instance.AccessCategories[i].IdAccessList;
			accessCategoriesList[i].Clear();
			accessCategoriesList[i].AddItems(accessList.Count);
			for (int j = 0; j < accessList.Count; j++)
			{
				GUI_IdConsoleEntryOld entryOld;
				entryOld = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntryOld;
				entryOld.SetUpAccess(this, console.TargetCard, accessList[j], IdConsoleManager.Instance.AccessCategories[i]);
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
				GUI_IdConsoleEntryOld entryOld;
				entryOld = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntryOld;
				entryOld.CheckIsSet();
			}
		}
	}

	//This method is super slow - call it only if the list is empty
	private void CreateAssignList()
	{
		assignList.Clear();
		assignList.AddItems(jobsCount);
		GUI_IdConsoleEntryOld entryOld;
		var occupations = OccupationList.Instance.Occupations.ToArray();
		for (int i = 0; i < jobsCount; i++)
		{
			JobType jobType = occupations[i].JobType;
			if (IdConsoleManager.Instance.IgnoredJobs.Contains(jobType))
			{
				continue;
			}
			entryOld = assignList.Entries[i] as GUI_IdConsoleEntryOld;
			entryOld.SetUpAssign(this, console.TargetCard, OccupationList.Instance.Get(jobType));
		}
	}

	private void UpdateAssignList()
	{
		GUI_IdConsoleEntryOld entryOld;
		for (int i = 0; i < jobsCount; i++)
		{
			entryOld = assignList.Entries[i] as GUI_IdConsoleEntryOld;
			entryOld.CheckIsSet();
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

	public void ChangeAssignment(Occupation occupationToSet)
	{
		console.TargetCard.accessSyncList.Clear();
		console.TargetCard.AddAccessList(occupationToSet.AllowedAccess);
		console.TargetCard.jobTypeInt = (int)occupationToSet.JobType;
		UpdateAssignment();
	}

	public void RemoveTargetCard()
	{
		if (console.TargetCard == null)
		{
			return;
		}
		console.EjectCard(console.TargetCard);
		pageSwitcher.SetActivePage(usercardPage);
	}

	public void RemoveAccessCard()
	{
		if (console.AccessCard == null)
		{
			return;
		}
		console.EjectCard(console.AccessCard);
		UpdateCardNames();
		LogOut();
	}

	public void LogIn()
	{
		if (console.AccessCard != null &&
			console.AccessCard.accessSyncList.Contains((int)Access.change_ids))
		{
			console.LoggedIn = true;
			pageSwitcher.SetActivePage(usercardPage);
			UpdateScreen();
		}
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
