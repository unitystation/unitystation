using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ServerInfo;
using DatabaseAPI;

namespace UI
{
	public class GUI_IngameMenu : MonoBehaviour
	{
		/// <summary>
		/// Menu window that will be deactivated when closing the menu.
		/// </summary>
		public GameObject menuWindow;

		public VotePopUp VotePopUp;

		public GameObject serverInfo;

		private ModalPanelManager modalPanelManager => ModalPanelManager.Instance;

		private CustomNetworkManager networkManager => CustomNetworkManager.Instance;
		public static GUI_IngameMenu Instance;

		private bool sentData;

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
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			Logger.Log("Opening " + nextMenuPanel.name + " menu", Category.UI);
			nextMenuPanel.SetActive(true);
		}

		/// <summary>
		/// Opens all menu panels (Menu and disclaimer)
		/// </summary>
		public void OpenMenuPanel()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			Logger.Log($"Opening {menuWindow.name} menu", Category.UI);
			menuWindow.SetActive(true);
			if (UIManager.Display.disclaimer != null) UIManager.Display.disclaimer.SetActive(true);

			if (!sentData)
			{
				sentData = true;
				ServerInfoMessageClient.Send(ServerData.UserID);
			}

			serverInfo.SetActive(false);
			if (string.IsNullOrEmpty(GetComponent<ServerInfoUI>().ServerDesc.text)) return;
			serverInfo.SetActive(true);
		}

		/// <summary>
		/// Closes a specific menu panel.
		/// </summary>
		/// <param name="thisPanel">The menu panel to close.</param>
		public void CloseMenuPanel(GameObject thisPanel)
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			Logger.Log("Closing " + thisPanel.name + " menu", Category.UI);
			thisPanel.SetActive(false);
		}

		/// <summary>
		/// Closes all menu panels (Menu and disclaimer)
		/// </summary>
		public void CloseMenuPanel(bool doSound = true)
		{
			if (doSound)
			{
				_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			}

			Logger.Log($"Closing {menuWindow.name} menu", Category.UI);
			HideAllMenus();
		}

		public void OpenOptionsScreen()
		{
			Unitystation.Options.OptionsMenu.Instance.Open();
			HideAllMenus();
		}

		public void InitiateRestartVote()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);

			if (PlayerManager.PlayerScript == null) return;
			if (PlayerManager.PlayerScript.PlayerNetworkActions == null) return;

			PlayerManager.PlayerScript.PlayerNetworkActions.CmdInitiateRestartVote();

			CloseMenuPanel();
		}

		#endregion

		#region Logout Confirmation Window Functions

		public void LogoutButton()
		{
			modalPanelManager.Confirm("Are you sure?", LogoutConfirmYesButton, "Logout");
		}

		public void LogoutConfirmYesButton()
		{
			EventManager.Broadcast(Event.RoundEnded);
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
			HideAllMenus();
			StopNetworking();
			SceneManager.LoadScene("Lobby");
		}

		#endregion

		#region Exit Confirmation Window Functions

		public void ExitButton()
		{
			modalPanelManager.Confirm("Are you sure?", ExitConfirmYesButton, "Exit");
		}

		public void ExitConfirmYesButton()
		{
			_ = SoundManager.Play(SingletonSOSounds.Instance.Click01);
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
			if (networkManager.isServer)
			{
				networkManager.StopHost();
				Logger.Log("Stopping host", Category.Connections);
			}
			else
			{
				networkManager.StopClient();
				Logger.Log("Stopping client", Category.Connections);
			}
		}

		private void HideAllMenus()
		{
			menuWindow.SetActive(false);
			serverInfo.SetActive(false);
			if (UIManager.Display.disclaimer != null) UIManager.Display.disclaimer.SetActive(false);
		}

		#endregion
	}
}
