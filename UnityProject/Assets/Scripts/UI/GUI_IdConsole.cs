using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_IdConsole : NetTab
{
	private IdConsole console;
	[SerializeField]
	private EmptyItemList accessList;
	[SerializeField]
	private EmptyItemList assignList;
	[SerializeField]
	private NetPageSwitcher pageSwitcher;
	[SerializeField]
	private NetPage loginPage;
	[SerializeField]
	private NetPage mainPage;

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
		//console.OnConsoleUpdate.AddListener(UpdateScreen);
		//UpdateScreen();
	}

	public void UpdateScreen()
	{
		accessList.Clear();
		accessList.AddItems(System.Enum.GetValues(typeof(Access)).Length);
		int i = 0;
		GUI_IdConsoleEntry entry;
		Debug.Log("Len " + System.Enum.GetValues(typeof(Access)).Length);
		foreach(Access access in Enum.GetValues(typeof(Access)))
		{
			Debug.Log("i " + i);
			entry = accessList.Entries[i] as GUI_IdConsoleEntry;
			entry.SetUp(this, console.TargetCard, access, null, false);
			i++;
		}
		//update both card names
		//update contained access on target card
		//update displayed cardholder name
		//update displayed cardholder assignment
	}

	public void ChangeName(string newName)
	{
		console.TargetCard.RegisteredName = newName;
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

	public void ChangeAssignment(Assignment assignmentToSet)
	{
		console.TargetCard.accessSyncList.Clear();
		console.TargetCard.AddAccessList(assignmentToSet.AccessList);
		console.TargetCard.jobTypeInt = (int)assignmentToSet.Job;
	}

	public void RemoveTargetCard()
	{
		if (console.TargetCard == null)
		{
			return;
		}
		console.EjectCard(console.TargetCard);
		console.TargetCard = null;
	}

	public void RemoveUserCard()
	{
		if (console.UserCard == null)
		{
			return;
		}
		console.EjectCard(console.UserCard);
		console.UserCard = null;
	}

	public void LogIn()
	{
		if (console.TargetCard != null &&
			console.TargetCard.accessSyncList.Contains((int)Access.change_ids))
		{
			console.LoggedIn = true;
			pageSwitcher.SetActivePage(mainPage);
			UpdateScreen();
		}
		//No access to console
	}

	public void LogOut()
	{
		RemoveTargetCard();
		console.LoggedIn = false;
		pageSwitcher.SetActivePage(loginPage);
	}

	public void CloseTab()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}

[Serializable]
public class Assignment
{
	public List<Access> AccessList;
	public JobType Job;
}
