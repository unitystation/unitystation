using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DatabaseAPI;

namespace AdminTools
{
	public class GUI_AdminTools : MonoBehaviour
	{
		[SerializeField] private GameObject retrievingDataScreen = null;

		[SerializeField] private GameObject backBtn = null;
		[SerializeField] private GameObject gameModePage = null;
		[SerializeField] private GameObject mainPage = null;
		[SerializeField] private GameObject playerManagePage = null;
		[SerializeField] private GameObject playerChatPage = null;
		[SerializeField] private GameObject playersScrollView = null;
		[SerializeField] private GameObject centCommPage = null;
		[SerializeField] private GameObject eventsManagerPage = null;
		[SerializeField] private GameObject roundManagerPage = null;
		private PlayerChatPage playerChatPageScript;
		private PlayerManagePage playerManagePageScript;
		public KickBanEntryPage kickBanEntryPage;
		public AreYouSurePage areYouSurePage;

		[SerializeField] private Transform playerListContent = null;
		[SerializeField] private GameObject playerEntryPrefab = null;

		[SerializeField] private Text windowTitle = null;

		private List<AdminPlayerEntry> playerEntries = new List<AdminPlayerEntry>();
		public string SelectedPlayer { get; private set; }

		private void OnEnable()
		{
			playerChatPageScript = playerChatPage.GetComponent<PlayerChatPage>();
			playerManagePageScript = playerManagePage.GetComponent<PlayerManagePage>();
			ShowMainPage();
		}

		public void ClosePanel()
		{
			gameObject.SetActive(false);
		}

		public void ToggleOnOff()
		{
			gameObject.SetActive(!gameObject.activeInHierarchy);
		}

		public void OnBackButton()
		{
			if (playerChatPage.activeInHierarchy)
			{
				playerChatPage.GetComponent<PlayerChatPage>().GoBack();
				return;
			}
			ShowMainPage();
		}

		public void ShowMainPage()
		{
			DisableAllPages();
			mainPage.SetActive(true);
			windowTitle.text = "ADMIN TOOL PANEL";
		}

		public void ShowGameModePage()
		{
			DisableAllPages();
			gameModePage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "GAME MODE MANAGER";
			retrievingDataScreen.SetActive(true);
		}

		public void ShowPlayerManagePage()
		{
			DisableAllPages();
			playerManagePage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "PLAYER MANAGER";
			playersScrollView.SetActive(true);
			retrievingDataScreen.SetActive(true);
		}

		public void ShowPlayerChatPage()
		{
			DisableAllPages();
			playerChatPage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "PLAYER BWOINK";
			playersScrollView.SetActive(true);
			retrievingDataScreen.SetActive(true);
		}

		public void ShowCentCommPage()
		{
			DisableAllPages();
			centCommPage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "CENTCOMM";
		}

		public void ShowEventsManagerPage()
		{
			DisableAllPages();
			eventsManagerPage.SetActive(true);
			eventsManagerPage.GetComponent<EventsManagerPage>().GenerateDropDownOptions();
			backBtn.SetActive(true);
			windowTitle.text = "EVENTS MANAGER";
		}

		public void ShowRoundManagerPage()
		{
			DisableAllPages();
			roundManagerPage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "ROUND MANAGER";
		}

		void DisableAllPages()
		{
			retrievingDataScreen.SetActive(false);
			gameModePage.SetActive(false);
			mainPage.SetActive(false);
			backBtn.SetActive(false);
			playerManagePage.SetActive(false);
			playerChatPage.SetActive(false);
			centCommPage.SetActive(false);
			eventsManagerPage.SetActive(false);
			playersScrollView.SetActive(false);
			roundManagerPage.SetActive(false);
			kickBanEntryPage.gameObject.SetActive(false);
			areYouSurePage.gameObject.SetActive(false);
		}

		public void CloseRetrievingDataScreen()
		{
			retrievingDataScreen.SetActive(false);
		}

		public void RefreshOnlinePlayerList(AdminPageRefreshData data)
		{
			foreach (var e in playerEntries)
			{
				Destroy(e.gameObject);
			}

			playerEntries.Clear();

			foreach (var p in data.players)
			{
				var e = Instantiate(playerEntryPrefab, playerListContent);
				var entry = e.GetComponent<AdminPlayerEntry>();
				entry.UpdateButton(p, SelectPlayerInList);

				if (p.isOnline)
				{
					entry.button.interactable = true;
				}
				else
				{
					if (!playerChatPage.activeInHierarchy)
					{
						entry.button.interactable = false;
					}
				}

				playerEntries.Add(entry);
				if (SelectedPlayer == p.uid)
				{
					entry.SelectPlayer();
					if (playerChatPage.activeInHierarchy)
					{
						playerChatPageScript.SetData(entry);
						SelectedPlayer = entry.PlayerData.uid;
					}

					if (playerManagePage.activeInHierarchy)
					{
						playerManagePageScript.SetData(entry);
					}
				}
			}

			if (string.IsNullOrEmpty(SelectedPlayer))
			{
				SelectPlayerInList(playerEntries[0]);
			}
		}

		public void SelectPlayerInList(AdminPlayerEntry selectedEntry)
		{
			foreach (var p in playerEntries)
			{
				if (p != selectedEntry)
				{
					p.DeselectPlayer();
				}
				else
				{
					p.SelectPlayer();
					SelectedPlayer = selectedEntry.PlayerData.uid;
				}
			}

			SelectedPlayer = selectedEntry.PlayerData.uid;

			if (playerChatPage.activeInHierarchy)
			{
				playerChatPageScript.SetData(selectedEntry);
			}

			if (playerManagePage.activeInHierarchy)
			{
				playerManagePageScript.SetData(selectedEntry);
			}
		}

		public void AddPendingMessagesToLogs(string playerId, List<AdminChatMessage> pendingMessages)
		{
			if (pendingMessages.Count == 0) return;

			playerChatPageScript.AddPendingMessagesToLogs(playerId, pendingMessages);
			if (playerId == SelectedPlayer)
			{
				playerChatPageScript.SetData(null);
			}
		}
	}
}