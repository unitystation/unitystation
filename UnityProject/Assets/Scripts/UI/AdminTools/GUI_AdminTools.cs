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

		[SerializeField] private Text windowTitle;

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
			retrievingDataScreen.SetActive(true);
		}

		public void ShowPlayerChatPage()
		{
			DisableAllPages();
			playerChatPage.SetActive(true);
			backBtn.SetActive(true);
			windowTitle.text = "PLAYER BWOINK";
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
		}

		public void CloseRetrievingDataScreen()
		{
			retrievingDataScreen.SetActive(false);
		}
	}
}