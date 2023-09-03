using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.Clearance;
using UnityEngine;
using UI.Core.NetUI;

namespace UI.Objects.Command
{
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
		private NetText_label targetCardName = null;
		[SerializeField]
		private NetText_label accessCardName = null;
		[SerializeField]
		private NetText_label loginCardName = null;

		//cached mapping from access to its corresponding entry for fast lookup
		private Dictionary<GUI_IDConsoleEntry, Clearance> accessToEntry = new Dictionary<GUI_IDConsoleEntry, Clearance>();
		private Dictionary<GUI_IDConsoleEntry, Occupation> occupationToEntry = new Dictionary<GUI_IDConsoleEntry, Occupation>();

		/// <summary>
		/// Card currently targeted for security modifications. Null if none inserted
		/// </summary>
		public IDCard TargetCard => console.TargetCard;

		private void Awake()
		{
			mainPage.SetActive(true);
			//cache the entries for quick lookup
			foreach (var entry in GetComponentsInChildren<GUI_IDConsoleEntry>())
			{
				if (entry.IsAccess)
				{
					accessToEntry.Add(entry, entry.Clearance);
				}
				else
				{
					occupationToEntry.Add(entry, entry.Occupation);
				}
			}
			mainPage.SetActive(false);
		}

		public override void OnEnable()
		{
			base.OnEnable();
			if (CustomNetworkManager.Instance._isServer)
			{
				StartCoroutine(ServerWaitForProvider());
			}
		}

		private IEnumerator ServerWaitForProvider()
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
			foreach (var entry in accessToEntry.Keys)
			{
				entry.ServerRefreshFromTargetCard();
			}

			foreach (var entry in occupationToEntry.Keys)
			{
				entry.ServerRefreshFromTargetCard();
			}
		}

		private void ServerUpdateLoginCardName()
		{
			loginCardName.MasterSetValue(console.AccessCard != null ?
				$"{console.AccessCard.RegisteredName}, {console.AccessCard.GetJobTitle()}" : "********");
		}

		private void ServerRefreshCardNames()
		{
			string valToSet = null;
			if (console.AccessCard != null && accessCardName)
			{
				valToSet = $"{console.AccessCard.RegisteredName}, {console.AccessCard.GetJobTitle()}";
			}
			else
			{
				valToSet = "-";
			}

			if (!valToSet.Equals(accessCardName.Value))
			{
				accessCardName.MasterSetValue(valToSet);
			}


			valToSet = console.TargetCard != null ? $"{console.TargetCard.RegisteredName}, {console.TargetCard.GetJobTitle()}" : "-";

			if (valToSet.Equals(targetCardName.Value) == false)
			{
				targetCardName.MasterSetValue(valToSet);
			}
		}

		public void ServerChangeName(string newName)
		{
			if (newName.Length <= 32)
			{
				console.TargetCard.ServerSetRegisteredName(newName);
				ServerRefreshCardNames();
				return;
			}

			Chat.AddExamineMsgToClient($"Name cannot exceed 32 characters!");
		}

		public void ServerChangeJobTitle(string newJobTitle)
		{
			if (newJobTitle.Length <= 32)
			{
				console.TargetCard.ServerSetJobTitle(newJobTitle);
				ServerRefreshCardNames();
			}
			else
			{
				Chat.AddExamineMsgToClient($"Job title cannot exceed 32 characters!");
				return;
			}
		}

		/// <summary>
		/// Grants the target card the given access
		/// </summary>
		/// <param name="accessToModify"></param>
		/// <param name="grant">if true, grants access, otherwise removes it</param>
		public void ServerModifyAccess(Clearance accessToModify, bool grant)
		{
			var idClearance = console.TargetCard.ClearanceSource;
			if (idClearance == null)
			{
				Loggy.LogError($"ID card {gameObject.name} has no BasicClearanceSource component!", Category.Objects);
				return;
			}

			var alreadyHasClearance = ((IClearanceSource)idClearance).GetCurrentClearance.Contains(accessToModify);

			switch (grant)
			{
				case false when alreadyHasClearance:
				{
					if (GameManager.Instance.CentComm.IsLowPop)
					{
						idClearance.ServerRemoveLowPopClearance(accessToModify);
						return;
					}

					idClearance.ServerRemoveClearance(accessToModify);
					break;
				}
				case true when alreadyHasClearance == false:
				{
					if (GameManager.Instance.CentComm.IsLowPop)
					{
						idClearance.ServerAddLowPopClearance(accessToModify);
						return;
					}

					idClearance.ServerAddClearance(accessToModify);
					break;
				}
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

		public void ServerRemoveTargetCard(PlayerInfo player)
		{
			if (console.TargetCard == null)
			{
				return;
			}
			console.EjectCard(console.TargetCard, player);
			pageSwitcher.SetActivePage(usercardPage);
		}

		public void ServerRemoveAccessCard(PlayerInfo player)
		{
			if (console.AccessCard == null)
			{
				return;
			}
			console.EjectCard(console.AccessCard, player);
			ServerRefreshCardNames();
			ServerLogOut(player);
		}

		public void ServerLogin()
		{
			var idClearance = console.AccessCard.OrNull()?.GetComponent<IClearanceSource>();
			if (idClearance != null && console.Restricted.HasClearance(idClearance) || IsAIInteracting())
			{
				console.LoggedIn = true;
				pageSwitcher.SetActivePage(usercardPage);
				ServerUpdateScreen();
			}
		}

		public void ServerLogOut(PlayerInfo player)
		{
			ServerRemoveTargetCard(player);
			console.LoggedIn = false;
			pageSwitcher.SetActivePage(loginPage);
			ServerUpdateLoginCardName();
			ServerRemoveAccessCard(player);
		}
	}
}
