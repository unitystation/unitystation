using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Old GUI_IDConsole, used only for stress testing nettab system
/// </summary>
public class GUI_IdConsoleOld : NetTab
{
	private IdConsole console;
	[SerializeField]
	private List<EmptyItemList> accessCategoriesList = null;
	[SerializeField]
	private EmptyItemList assignList = null;
	[SerializeField]
	private NetPageSwitcher pageSwitcher = null;
	[SerializeField]
	private NetPage loginPage = null;
	[SerializeField]
	private NetPage usercardPage = null;
	[SerializeField]
	private NetPage mainPage = null;
	[SerializeField]
	private NetLabel targetCardName = null;
	[SerializeField]
	private NetLabel accessCardName = null;
	[SerializeField]
	private NetLabel loginCardName = null;
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
		loginCardName.SetValueServer(console.AccessCard != null ?
			$"{console.AccessCard.RegisteredName}, {console.AccessCard.JobType.ToString()}" : "********");
	}

	private void UpdateCardNames()
	{
		if (console.AccessCard != null)
		{
			accessCardName.SetValueServer($"{console.AccessCard.RegisteredName}, {console.AccessCard.JobType.ToString()}");
		}
		else
		{
			accessCardName.SetValueServer("-");
		}

		if (console.TargetCard != null)
		{
			targetCardName.SetValueServer($"{console.TargetCard.RegisteredName}, {console.TargetCard.JobType.ToString()}");
		}
		else
		{
			targetCardName.SetValueServer("-");
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
		for (int i = 0; i < IdConsoleManagerOld.Instance.AccessCategories.Count; i++)
		{
			List<IdAccess> accessList = IdConsoleManagerOld.Instance.AccessCategories[i].IdAccessList;
			accessCategoriesList[i].Clear();
			accessCategoriesList[i].AddItems(accessList.Count);
			for (int j = 0; j < accessList.Count; j++)
			{
				GUI_IdConsoleEntryOld entryOld;
				entryOld = accessCategoriesList[i].Entries[j] as GUI_IdConsoleEntryOld;
				entryOld.SetUpAccess(this, console.TargetCard, accessList[j], IdConsoleManagerOld.Instance.AccessCategories[i]);
			}
		}
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
			if (IdConsoleManagerOld.Instance.IgnoredJobs.Contains(jobType))
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
		console.TargetCard.ServerSetRegisteredName(newName);
		UpdateCardNames();
	}

	/// <summary>
	/// Grants the target card the given access
	/// </summary>
	/// <param name="accessToModify"></param>
	/// <param name="grant">if true, grants access, otherwise removes it</param>
	public void ModifyAccess(Access accessToModify)
	{
		if (console.TargetCard.HasAccess(accessToModify))
		{
			console.TargetCard.ServerRemoveAccess(accessToModify);
		}
		else
		{
			console.TargetCard.ServerAddAccess(accessToModify);
		}
	}

	public void ChangeAssignment(Occupation occupationToSet)
	{
		console.TargetCard.ServerChangeOccupation(occupationToSet);
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
			console.AccessCard.HasAccess(Access.change_ids))
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
}
