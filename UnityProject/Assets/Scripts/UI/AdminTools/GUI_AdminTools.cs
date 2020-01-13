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

		[SerializeField] private GameObject mainMenuBtn;
		[SerializeField] private GameObject gameModePage;
		[SerializeField] private GameObject mainPage;

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
			SoundManager.Play("Click01");
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
			SoundManager.Play("Click01");
			mainMenuBtn.SetActive(true);
			windowTitle.text = "GAME MODE MANAGER";
			retrievingDataScreen.SetActive(true);
		}

		void DisableAllPages()
		{
			retrievingDataScreen.SetActive(false);
			gameModePage.SetActive(false);
			mainPage.SetActive(false);
			mainMenuBtn.SetActive(false);
		}
	}
}