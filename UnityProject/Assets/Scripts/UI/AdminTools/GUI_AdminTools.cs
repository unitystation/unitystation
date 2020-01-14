using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AdminTools
{
	public class GUI_AdminTools : MonoBehaviour
	{
		[SerializeField] private GameObject retrievingDataScreen;

		[SerializeField] private GameObject backBtn;
		[SerializeField] private GameObject gameModePage;
		[SerializeField] private GameObject mainPage;
		[SerializeField] private GameObject playerManagePage;
		[SerializeField] private GameObject playerChatPage;
		[SerializeField] private GameObject playersScrollView;
		public KickBanEntryPage kickBanEntryPage;
		public AreYouSurePage areYouSurePage;

		[SerializeField] private Transform playerListContent;
		[SerializeField] private GameObject playerEntryPrefab;

		[SerializeField] private Text windowTitle;

		private List<AdminPlayerEntry> playerEntries = new List<AdminPlayerEntry>();
		private string selectedPlayer;

		private void OnEnable()
		{
			ShowMainPage();
		}

		public void ClosePanel()
		{
			gameObject.SetActive(false);
		}

		public void OnBackButton()
		{
			ShowMainPage();
		}

		private void ShowMainPage()
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

		void DisableAllPages()
		{
			retrievingDataScreen.SetActive(false);
			gameModePage.SetActive(false);
			mainPage.SetActive(false);
			backBtn.SetActive(false);
			playerManagePage.SetActive(false);
			playerChatPage.SetActive(false);
			playersScrollView.SetActive(false);
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
				entry.UpdateButton(p, this);

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
				if (selectedPlayer == p.uid)
				{
					entry.SelectPlayer();
				}
			}

			if (string.IsNullOrEmpty(selectedPlayer))
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
					selectedPlayer = selectedEntry.PlayerData.uid;
				}
			}

			if (playerChatPage.activeInHierarchy)
			{
				playerChatPage.GetComponent<PlayerChatPage>().SetData(selectedEntry);
			}

			if (playerManagePage.activeInHierarchy)
			{
				playerManagePage.GetComponent<PlayerManagePage>().SetData(selectedEntry);
			}
		}
	}
}