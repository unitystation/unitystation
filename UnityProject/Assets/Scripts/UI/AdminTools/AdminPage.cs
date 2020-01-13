using System;
using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;

namespace AdminTools
{
	public class AdminPage : MonoBehaviour
	{
		protected AdminPageRefreshData currentData;
		protected GUI_AdminTools adminTools;

		public virtual void OnEnable()
		{
			if (adminTools == null)
			{
				adminTools = FindObjectOfType<GUI_AdminTools>();
			}
			RefreshPage();
		}

		public void RefreshPage()
		{
			RequestAdminPageRefresh.Send(ServerData.UserID, PlayerList.Instance.AdminToken);
		}

		public virtual void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			currentData = adminPageData;
			adminTools.CloseRetrievingDataScreen();
		}
	}

	[Serializable]
	public class AdminPageRefreshData
	{
		//GameMode updates:
		public List<string> availableGameModes = new List<string>();
		public string currentGameMode;
		public bool isSecret;
		public string nextGameMode;

	}
}