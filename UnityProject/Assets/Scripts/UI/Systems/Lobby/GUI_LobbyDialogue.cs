using UnityEngine;
using UnityEngine.UI;
using Systems.Character;

namespace Lobby
{
	public class GUI_LobbyDialogue :  MonoBehaviour
	{
		#region Inspector fields

		[SerializeField]
		private Text dialogueTitle = default;
		[SerializeField]
		private Transform panelContainer;

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

		private void Start()
		{
			DeterminePanel();
		}

		private void OnEnable()
		{
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

		public void ShowAlphaPanel()
		{
			HideAllPanels();
			SetTitle("Alpha");
			informationPanel.SetActive(true);
		}

		public void ShowControlInformationPanel()
		{
			HideAllPanels();
			SetTitle("Controls");
			controlInformationPanel.SetActive(true);
		}

		public void ShowLoadingPanel(string loadingMessage)
		{
			ShowLoadingPanel(new LoadingPanelArgs
			{
				Text = loadingMessage,
			});
		}

		public void ShowLoginError(string msg)
		{
			var infoArgs = new InfoPanelArgs
			{
				IsError = true,
				Heading = "Sign-in Failed",
				Text = msg,
				LeftButtonLabel = "Back",
				LeftButtonCallback = ShowLoginPanel,
			};

			if (msg.Contains("Email Not Verified"))
			{
				infoArgs.RightButtonLabel = "Resend Email";
				infoArgs.RightButtonCallback = LobbyManager.Instance.ResendVerifyEmail;
			}

			ShowInfoPanel(infoArgs);
		}

		#endregion

		private void SetTitle(string title) => dialogueTitle.text = title;

		private void ClearTitle() => dialogueTitle.text = string.Empty;

		private void HideAllPanels()
		{
			ClearTitle();

			foreach (Transform panel in panelContainer)
			{
				panel.SetActive(false);
			}
		}

		private void DeterminePanel()
		{
			HideAllPanels();

			if (PlayerManager.Account.IsAvailable == false)
			{
				ShowAlphaPanel();
			}
			else if (LobbyManager.Instance.WasDisconnected && GameManager.Instance.DisconnectExpected == false)
			{
				ShowInfoPanel(new InfoPanelArgs
				{
					IsError = true,
					Heading = "Lost Connection",
					Text = "Lost connection to the server. Check your console (F5).",
					LeftButtonLabel = "Back",
					LeftButtonCallback = ShowMainPanel,
					RightButtonLabel = "Rejoin",
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
	}
}
