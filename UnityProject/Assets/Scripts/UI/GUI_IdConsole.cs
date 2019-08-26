using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUI_IdConsole : NetTab
{
	private IdConsole console;

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
			//switch page
		}
		//No access to console
	}

	public void LogOut()
	{
		RemoveTargetCard();
		console.LoggedIn = false;
		//switch page
	}
}

[Serializable]
public class Assignment
{
	public List<Access> AccessList;
	public JobType Job;
}
