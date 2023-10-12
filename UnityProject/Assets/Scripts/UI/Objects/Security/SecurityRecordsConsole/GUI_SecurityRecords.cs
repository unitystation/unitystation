﻿using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Systems.Character;
using UI.Core.NetUI;
using Objects.Security;
using Systems;

namespace UI.Objects.Security
{
	public class GUI_SecurityRecords : NetTab
	{
		[SerializeField]
		private NetPageSwitcher nestedSwitcher = null;
		[SerializeField]
		private GUI_SecurityRecordsEntriesPage entriesPage = null;
		[SerializeField]
		private GUI_SecurityRecordsEntryPage entryPage = null;
		[SerializeField]
		private NetText_label idText = null;
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

		public void RemoveId(PlayerInfo player)
		{
			if (console.IdCard)
			{
				console.ServerRemoveIDCard(player);
				UpdateScreen();
			}
			else if (IsAIInteracting())
			{
				UpdateScreen();
			}
		}

		public void UpdateIdText(NetText_label labelToSet)
		{
			var IdCard = console.IdCard;
			if (IdCard)
			{
				labelToSet.MasterSetValue($"{IdCard.RegisteredName}, {IdCard.GetJobTitle()}");
			}
			else if (IsAIInteracting())
			{
				labelToSet.MasterSetValue("AI Control");
			}
			else
			{
				labelToSet.MasterSetValue("********");
			}
		}

		public void LogIn()
		{
			if ((console.IdCard == null || console.Restricted.HasClearance(console.IdCard.ClearanceSource) == false) && IsAIInteracting() == false)
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
}

namespace Objects.Security
{
	public enum SecurityStatus
	{
		None,
		Arrest,
		Criminal,
		Parole
	}

	[System.Serializable]
	public class SecurityRecord
	{
		public static event Action OnWantedLevelChange;

		private string entryName;

		public string EntryName
		{
			get => entryName;
			set
			{
				CrewManifestManager.Instance.OrNull()?.UpdateNameSecurityRecord(this, value);
				entryName = value;
			}
		}

		public string ID;
		public string Sex;
		public string Age;
		public string Species;
		public string Rank;
		public string Fingerprints;


		public SecurityStatus status;

		public SecurityStatus Status
		{
			get => status;
			set
			{
				bool diff = status != value;

				status = value;
				if (diff)
				{
					try
					{
						OnWantedLevelChange?.Invoke();
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());

					}
				}
			}
		}


		public List<SecurityRecordCrime> Crimes;
		public Occupation Occupation;
		public CharacterSheet characterSettings;

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
}
