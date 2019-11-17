
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Optimized, new GUI_IDConsole
/// </summary>
public class GUI_IDConsole : NetTab
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

	/// <summary>
	/// Card currently targeted for security modifications. Null if none inserted
	/// </summary>
	public IDCard TargetCard => console.TargetCard;

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(WaitForProvider());
			jobsCount = OccupationList.Instance.Occupations.Count() - IdConsoleManagerOld.Instance.IgnoredJobs.Count;
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

	private void UpdateAccessList()
	{
		for (int i = 0; i < IdConsoleManagerOld.Instance.AccessCategories.Count; i++)
		{
			List<IdAccess> accessList = IdConsoleManagerOld.Instance.AccessCategories[i].IdAccessList;
			for (int j = 0; j < accessList.Count; j++)
			{
				GUI_IdConsoleEntryOld entryOld;
				entryOld = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntryOld;
				entryOld.CheckIsSet();
			}
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

	/// <summary>
	/// Grants the target card the given access
	/// </summary>
	/// <param name="accessToModify"></param>
	/// <param name="grant">if true, grants access, otherwise removes it</param>
	public void ModifyAccess(Access accessToModify, bool grant)
	{
		if (!grant && console.TargetCard.accessSyncList.Contains((int) accessToModify))
		{
			console.TargetCard.accessSyncList.Remove((int)accessToModify);
		}
		else if (grant)
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

