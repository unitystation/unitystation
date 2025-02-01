using System;
using System.Collections.Generic;
using DatabaseAPI;
using Messages.Client.Admin;
using UnityEngine;
using UnityEngine.Serialization;

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
				adminTools = FindObjectOfType<GUI_AdminTools>(); // TODO This causes a ~80ms frame hitch when the page is opened.
			}
			RefreshPage();
		}

		public void RefreshPage()
		{
			RequestAdminPageRefresh.Send();
		}

		public virtual void OnPageRefresh(AdminPageRefreshData adminPageData)
		{
			currentData = adminPageData;
			adminTools.RefreshOnlinePlayerList(adminPageData);
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

		//Event Manager Updates:
		public bool randomEventsAllowed;

		//Round Manager
		public string nextMap;
		public string nextAwaySite;
		public bool allowLavaLand;
		public string alertLevel;

		//Centcom
		public bool blockCall;
		public bool blockRecall;

		//Player Management:
		public List<AdminPlayerEntryData> players = new List<AdminPlayerEntryData>();

		//Server Settings
		public int playerLimit;
		public int maxFrameRate;
		public string serverPassword;
	}

	[Serializable]
	public class AdminPlayerEntryData
	{
		public string name;
		public string uid;
		public string currentJob;
		public string accountName;
		public bool isAlive;
		public bool isAntag;
		public bool hasAChat; //needs for achat (tag achat) , roll ui tag
		public string roleSmall; // roll ui tag
		public string roleColour; // roll ui tag
		public bool hasMentorRole; //has metnor roll
		public bool isOnline;
		public bool isOOCMuted;
		public string ipAddress;
		public uint playerObject;
	}

	[Serializable]
	public class AdminChatMessage : ChatEntryData
	{
		public string fromUserid;
		public bool wasFromAdmin;
	}

	[Serializable]
	public class AdminChatUpdate
	{
		public List<AdminChatMessage> messages = new List<AdminChatMessage>();
	}
}