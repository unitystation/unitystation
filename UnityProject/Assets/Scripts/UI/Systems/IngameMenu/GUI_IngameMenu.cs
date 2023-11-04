using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Learning;
using Logs;
using Messages.Client.Lobby;
using SecureStuff;
using UI.Chat_UI;


namespace UI
{
	public class GUI_IngameMenu : MonoBehaviour
	{
		/// <summary>
		/// Menu window that will be deactivated when closing the menu.
		/// </summary>
		public GameObject menuWindow;
		public GameObject votingWindow;
		public GameObject helpWindow;

		public VotePopUp VotePopUp;

		private ModalPanelManager ModalPanelManager => ModalPanelManager.Instance;

		private CustomNetworkManager NetworkManager => CustomNetworkManager.Instance;
		public static GUI_IngameMenu Instance;

		private bool sentData;

		[SerializeField] private string wikiURL = "https://wiki.unitystation.org/index.php/Welcome_to_Unitystation";

		#region Lifecycle

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void OnEnable()
		{
			SceneManager.activeSceneChanged += OnSceneLoaded;
		}

		void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnSceneLoaded;
		}

		void OnSceneLoaded(Scene oldScene, Scene newScene)
		{
			if (newScene.name != "Lobby")
			{
				CloseMenuPanel(); // Close disclaimer and menu on scene switch
			}
		}

		#endregion

#if UNITY_EDITOR
		[NonSerialized]
		public bool isTest = false;
#endif

		#region Main Ingame Menu Functions

		/// <summary>
		/// Opens a specific menu panel.
		/// </summary>
		/// <param name="nextMenuPanel">Menu panel to open</param>
		public void OpenMenuPanel(GameObject nextMenuPanel)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			Loggy.Log("Opening " + nextMenuPanel.name + " menu", Category.UI);
			nextMenuPanel.SetActive(true);
		}

		/// <summary>
		/// Opens all menu panels (Menu and disclaimer)
		/// </summary>
		public void OpenMenuPanel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			Loggy.Log($"Opening {menuWindow.name} menu", Category.UI);
			menuWindow.SetActive(true);
			InfoPanelMessageClient.Send();
			if (UIManager.Instance.ServerInfoPanelWindow != null) UIManager.Instance.ServerInfoPanelWindow.SetActive(true);
		}

		/// <summary>
		/// Closes a specific menu panel.
		/// </summary>
		/// <param name="thisPanel">The menu panel to close.</param>
		public void CloseMenuPanel(GameObject thisPanel)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			Loggy.Log("Closing " + thisPanel.name + " menu", Category.UI);
			thisPanel.SetActive(false);
		}

		/// <summary>
		/// Closes all menu panels (Menu and disclaimer)
		/// </summary>
		public void CloseMenuPanel(bool doSound = true)
		{
			if (doSound)
			{
				_ = SoundManager.Play(CommonSounds.Instance.Click01);
			}

			Loggy.Log($"Closing {menuWindow.name} menu", Category.UI);
			HideAllMenus();
		}

		public void OpenOptionsScreen()
		{
			Unitystation.Options.OptionsMenu.Instance.Open();
			HideAllMenus();
		}

		public void InitiateRestartVote()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (PlayerManager.LocalPlayerScript == null) return;
			if (PlayerManager.LocalPlayerScript.PlayerNetworkActions == null) return;

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdInitiateRestartVote();

			CloseMenuPanel();
		}

		public void InitiateMapVote()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (PlayerManager.LocalPlayerScript == null) return;
			if (PlayerManager.LocalPlayerScript.PlayerNetworkActions == null) return;

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdInitiateMapVote();

			CloseMenuPanel();
		}

		public void InitiateGameModeVote()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (PlayerManager.LocalPlayerScript == null) return;
			if (PlayerManager.LocalPlayerScript.PlayerNetworkActions == null) return;

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdInitiateGameModeVote();

			CloseMenuPanel();
		}

		public void ShowVoteOptions()
		{
			HideAllMenus();
			votingWindow.SetActive(true);
		}

		public void ShowHelpMenu()
		{
			HideAllMenus();
			helpWindow.SetActive(true);
		}

		public void ShowProtipListUI()
		{
			ProtipManager.Instance.ShowListUI();
		}

		public void ShowMentorHelp()
		{
			HideAllMenus();
			ChatUI.Instance.OnHelpButton();
		}

		public void OpenWiki()
		{
			HideAllMenus();
			SafeURL.Open(wikiURL);
		}

		#endregion

		#region Logout Confirmation Window Functions

		public void ExitToMainMenuBtn()
		{
			ModalPanelManager.Confirm("Are you sure?", LogoutConfirmYesButton, "Logout to Main Menu");
		}

		public void LogoutConfirmYesButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			EventManager.Broadcast(Event.RoundEnded);
			HideAllMenus();
			GameManager.Instance.DisconnectExpected = true;
			StopNetworking();
			SceneManager.LoadScene("Lobby");
		}

		#endregion

		#region Exit Confirmation Window Functions

		public void ExitButton()
		{
			ModalPanelManager.Confirm("Are you sure?", ExitConfirmYesButton, "Exit");
		}

		public void ExitConfirmYesButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			StopNetworking();
			// Either shutdown the application or stop the editor
#if UNITY_EDITOR
			if (isTest) return;
			UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
		}

		#endregion

		#region Misc. Functions

		private void StopNetworking()
		{
			// Check if a host or regular client is shutting down
			if (NetworkManager._isServer)
			{
				NetworkManager.StopHost();
				Loggy.Log("Stopping host", Category.Connections);
			}
			else
			{
				NetworkManager.StopClient();
				Loggy.Log("Stopping client", Category.Connections);
			}
		}

		private void HideAllMenus()
		{
			menuWindow.SetActive(false);
			votingWindow.SetActive(false);
			helpWindow.SetActive(false);
			if (UIManager.Display.disclaimer != null) UIManager.Display.disclaimer.SetActive(false);
		}

		#endregion
	}
}
