
using System;
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

	//cached mapping from access to its corresponding entry for fast lookup
	private Dictionary<Access,GUI_IDConsoleEntry> accessToEntry = new Dictionary<Access, GUI_IDConsoleEntry>();
	private Dictionary<Occupation,GUI_IDConsoleEntry> occupationToEntry = new Dictionary<Occupation, GUI_IDConsoleEntry>();

	/// <summary>
	/// Card currently targeted for security modifications. Null if none inserted
	/// </summary>
	public IDCard TargetCard => console.TargetCard;

	private void Awake()
	{
		//cache the entries for quick lookup
		foreach (var entry in GetComponentsInChildren<GUI_IDConsoleEntry>())
		{
			if (entry.IsAccess)
			{
				accessToEntry.Add(entry.Access, entry);
			}
			else
			{
				occupationToEntry.Add(entry.Occupation, entry);
			}
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(ServerWaitForProvider());
		}
	}

	IEnumerator ServerWaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}

		console = Provider.GetComponentInChildren<IdConsole>();
		console.OnConsoleUpdate.AddListener(ServerUpdateScreen);
		ServerUpdateScreen();
	}

	public void ServerUpdateScreen()
	{
		if (pageSwitcher.CurrentPage == loginPage)
		{
			ServerUpdateLoginCardName();
			ServerLogin();
		}
		if (pageSwitcher.CurrentPage == usercardPage && console.TargetCard != null)
		{
			pageSwitcher.SetActivePage(mainPage);
		}
		if (pageSwitcher.CurrentPage == mainPage)
		{

			ServerRefreshEntries();
		}
		ServerRefreshCardNames();
	}

	/// <summary>
	/// Goes through each entry and updates its status based on the inserted card
	/// </summary>
	private void ServerRefreshEntries()
	{
		foreach (var entry in accessToEntry.Values.Concat(occupationToEntry.Values))
		{
			entry.ServerRefreshFromTargetCard();
		}
	}

	private void ServerUpdateLoginCardName()
	{
		loginCardName.SetValueServer(console.AccessCard != null ?
			$"{console.AccessCard.RegisteredName}, {console.AccessCard.JobType.ToString()}" : "********");
	}

	private void ServerRefreshCardNames()
	{
		string valToSet = null;
		if (console.AccessCard != null && accessCardName)
		{
			valToSet = $"{console.AccessCard.RegisteredName}, {console.AccessCard.JobType.ToString()}";
		}
		else
		{
			valToSet = "-";
		}

		if (!valToSet.Equals(accessCardName.Value))
		{
			accessCardName.SetValueServer(valToSet);
		}


		if (console.TargetCard != null)
		{
			valToSet = $"{console.TargetCard.RegisteredName}, {console.TargetCard.JobType.ToString()}";
		}
		else
		{
			valToSet = "-";
		}

		if (!valToSet.Equals(targetCardName.Value))
		{
			targetCardName.SetValueServer(valToSet);
		}
	}

	public void ServerChangeName(string newName)
	{
		console.TargetCard.ServerSetRegisteredName(newName);
		ServerRefreshCardNames();
	}

	/// <summary>
	/// Grants the target card the given access
	/// </summary>
	/// <param name="accessToModify"></param>
	/// <param name="grant">if true, grants access, otherwise removes it</param>
	public void ServerModifyAccess(Access accessToModify, bool grant)
	{
		var alreadyHasAccess = console.TargetCard.HasAccess(accessToModify);
		if (!grant && alreadyHasAccess)
		{
			console.TargetCard.ServerRemoveAccess(accessToModify);
		}
		else if (grant && !alreadyHasAccess)
		{
			console.TargetCard.ServerAddAccess(accessToModify);
		}
	}

	public void ServerChangeAssignment(Occupation occupationToSet)
	{
		if (console.TargetCard.Occupation != occupationToSet)
		{
			console.TargetCard.ServerChangeOccupation(occupationToSet, true);
			ServerRefreshEntries();
			ServerRefreshCardNames();
		}
	}

	public void ServerRemoveTargetCard()
	{
		if (console.TargetCard == null)
		{
			return;
		}
		console.EjectCard(console.TargetCard);
		pageSwitcher.SetActivePage(usercardPage);
	}

	public void ServerRemoveAccessCard()
	{
		if (console.AccessCard == null)
		{
			return;
		}
		console.EjectCard(console.AccessCard);
		ServerRefreshCardNames();
		ServerLogOut();
	}

	public void ServerLogin()
	{
		if (console.AccessCard != null &&
			console.AccessCard.HasAccess(Access.change_ids))
		{
			console.LoggedIn = true;
			pageSwitcher.SetActivePage(usercardPage);
			ServerUpdateScreen();
		}
	}

	public void ServerLogOut()
	{
		ServerRemoveTargetCard();
		console.LoggedIn = false;
		pageSwitcher.SetActivePage(loginPage);
		ServerUpdateLoginCardName();
		ServerRemoveAccessCard();
	}
}
