using System;
using System.Collections.Generic;
using AdminTools;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.AdminTools
{
	public class AdminRespawnPage: AdminPage
	{
		[SerializeField][Tooltip("Game object that corresponds to this tab")]
		private RespawnTab normalJobTab = default;
		[SerializeField][Tooltip("Game object that corresponds to this tab")]
		private RespawnTab SpecialJobTab = default;
		[SerializeField][Tooltip("Game object that corresponds to this tab")]
		private RespawnTab antagTab = default;

		private RespawnTab activeTab;

		private void Awake()
		{
			PopulateNormalJobsDropdown();
			PopulateSpecialJobsDropdown();
			PopulateAntagsDropdown();
		}

		public override void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			base.OnPageRefresh(adminPageData);
			ChangeTab((int) RespawnPageTab.Job);
		}


		private void PopulateNormalJobsDropdown()
		{
			var optionData = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData
				{
					text = "Select a job..."
				}
			};

			foreach (var job in OccupationList.Instance.Occupations)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = job.DisplayName
				});
			}

			normalJobTab.Dropdown.value = 0;
			normalJobTab.Dropdown.options = optionData;
		}

		private void PopulateSpecialJobsDropdown()
		{
			var optionData = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData
				{
					text = "Select a special job..."
				}
			};

			foreach (var job in SOAdminJobsList.Instance.SpecialJobs)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = job.DisplayName
				});
			}

			SpecialJobTab.Dropdown.value = 0;
			SpecialJobTab.Dropdown.options = optionData;
		}

		private void PopulateAntagsDropdown()
		{
			var optionData = new List<Dropdown.OptionData>
			{
				new Dropdown.OptionData
				{
					text = "Select an antag..."
				}
			};

			foreach (var antag in SOAdminJobsList.Instance.Antags)
			{
				optionData.Add(new Dropdown.OptionData
				{
					text = antag.AntagName
				});
			}

			antagTab.Dropdown.value = 0;
			antagTab.Dropdown.options = optionData;
		}

		public void SetTabsWithPlayerEntry(AdminPlayerEntry playerEntry)
		{
			normalJobTab.SetPlayerEntry(playerEntry);
			SpecialJobTab.SetPlayerEntry(playerEntry);
			antagTab.SetPlayerEntry(playerEntry);
		}

		public void ChangeTab(int tabNumber)
		{
			HideAllTabs();

			var respawnPageTab = (RespawnPageTab) tabNumber;

			switch (respawnPageTab)
			{
				case RespawnPageTab.Job:
					normalJobTab.gameObject.SetActive(true);
					activeTab = normalJobTab;
					adminTools.WindowTitle.text = "NORMAL JOB RESPAWN";
					break;
				case RespawnPageTab.SpecialJob:
					SpecialJobTab.gameObject.SetActive(true);
					activeTab = SpecialJobTab;
					adminTools.WindowTitle.text = "SPECIAL JOB RESPAWN";
					break;
				case RespawnPageTab.Antag:
					antagTab.gameObject.SetActive(true);
					activeTab = antagTab;
					adminTools.WindowTitle.text = "ANTAG RESPAWN";
					break;
			}
		}

		private void HideAllTabs()
		{
			normalJobTab.gameObject.SetActive(false);
			SpecialJobTab.gameObject.SetActive(false);
			antagTab.gameObject.SetActive(false);
		}

		public void OnTabConfirmButton()
		{
			activeTab.RequestRespawn();
		}

		public void OnTabCancelButton()
		{
			adminTools.ShowMainPage();
		}
	}

	public enum RespawnPageTab
	{
		Job = 0,
		SpecialJob = 1,
		Antag = 2
	}
}
