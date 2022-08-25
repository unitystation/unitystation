using System;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class GUI_LobbyDialogue :  MonoBehaviour
	{
		#region Inspector fields

		[SerializeField]
		private Text dialogueTitle = default;

		// Static panels
		[SerializeField]
		private GameObject informationPanel = default;
		[SerializeField]
		private GameObject controlInformationPanel = default;

		// UI scripts
		[SerializeField]
		private LoadingPanel loadingPanelScript = default;
		[SerializeField]
		private InfoPanel infoPanelScript = default;
		[SerializeField]
		private MainMenuPanel mainMenuScript = default;
		[SerializeField]
		private AccountLoginPanel accountLoginScript = default;
		[SerializeField]
		private AccountCreatePanel accountCreateScript = default;
		[SerializeField]
		private JoinPanel joinScript = default;
		[SerializeField]
		private ServerHistoryPanel serverHistoryScript = default;

		#endregion

		public AccountLoginPanel LoginUIScript => accountLoginScript;

		#region Lifecycle

		private void OnEnable()
		{
			DeterminePanel();

			//login skip only allowed (and only works properly) in offline mode
			if (GameData.Instance.OfflineMode)
			{
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			}
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (Input.GetKeyDown(KeyCode.F6))
			{
				//skip login
				ShowMainPanel();
				//if there aren't char settings, default
				if (PlayerManager.CurrentCharacterSheet == null)
				{
					PlayerManager.CurrentCharacterSheet = new CharacterSheet();
				}
			}
		}

		#endregion

		#region Show Panels

		public void ShowInfoPanel(InfoPanelArgs args)
		{
			HideAllPanels();
			infoPanelScript.SetActive(true);
			infoPanelScript.Show(args);
		}

		public void ShowLoadingPanel(LoadingPanelArgs args)
		{
			HideAllPanels();
			loadingPanelScript.SetActive(true);
			loadingPanelScript.Show(args);
		}

		public void ShowMainPanel()
		{
			HideAllPanels();
			SetTitle("Unitystation");
			mainMenuScript.SetActive(true);
		}

		public void ShowLoginPanel()
		{
			HideAllPanels();
			SetTitle("Account Login");
			accountLoginScript.SetActive(true);
		}

		public void ShowAccountCreatePanel()
		{
			HideAllPanels();
			SetTitle("Create Account");
			accountCreateScript.SetActive(true);
		}

		public void ShowJoinPanel()
		{
			HideAllPanels();
			SetTitle("Join Server");
			joinScript.SetActive(true);
		}

		public void ShowServerHistoryPanel()
		{
			HideAllPanels();
			SetTitle("Server History");
			serverHistoryScript.SetActive(true);
		}

		#endregion

		private void SetTitle(string title) => dialogueTitle.text = title;

		private void ClearTitle() => dialogueTitle.text = string.Empty;

		private void HideAllPanels()
		{
			ClearTitle();

			foreach (GameObject panel in transform)
			{
				panel.SetActive(false);
			}
		}

		private void DeterminePanel()
		{
			HideAllPanels();

			if (ServerData.Auth?.CurrentUser == null)
			{
				ShowLoginPanel();
			}
			else if (LobbyManager.Instance.WasDisconnected && GameManager.Instance.DisconnectExpected == false)
			{
				ShowInfoPanel(new InfoPanelArgs
				{
					IsError = true,
					Heading = "Lost Connection",
					Text = "Lost connection to the server. Check your console (F5).",
					LeftButtonText = "Back",
					LeftButtonCallback = ShowMainPanel,
					RightButtonText = "Rejoin",
					RightButtonCallback = LobbyManager.Instance.ConnectToLastServer,
				});
			}
			else
			{
				ShowMainPanel();
			}

			// reset
			LobbyManager.Instance.WasDisconnected = false;
			GameManager.Instance.DisconnectExpected = false;
		}

		public void ShowLoadingPanel(string loadingMessage)
		{
			HideAllPanels();

			ShowLoadingPanel(new LoadingPanelArgs
			{
				Text = loadingMessage,
			});
		}

		// TODO not needed?
		public void LoginSuccess()
		{
			ShowMainPanel();
		}

		public void LoginError(string msg)
		{
			// TODO use ShowInfoPanel()

			/*
			loggingInText.text = $"Login failed: {msg}";
			if (msg.Contains("Email Not Verified"))
			{
				resendEmailButton.gameObject.SetActive(true);
				resendEmailButton.interactable = true;
			}
			else
			{
				resendEmailButton.gameObject.SetActive(false);
				ServerData.Auth.SignOut();
			}

			loginGoBackButton.SetActive(true);
			*/
		}

		public void ShowInformationPanel()
		{
			HideAllPanels();
			informationPanel.SetActive(true);
			dialogueTitle.text = "Alpha";
		}

		public void ShowControlInformationPanel()
		{
			HideAllPanels();
			controlInformationPanel.SetActive(true);
			dialogueTitle.text = "Controls";
		}

		public void ShowWrongVersionPanel() // TODO
		{
			HideAllPanels();

			// TODO use ShowInfoPanel()
			//wrongVersionPanel.SetActive(true);
			dialogueTitle.text = "Wrong Version";
		}
	}
}
