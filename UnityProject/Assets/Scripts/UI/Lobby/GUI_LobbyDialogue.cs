using System.Collections;
using System.Text.RegularExpressions;
using DatabaseAPI;
using Facepunch.Steamworks;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
	public class GUI_LobbyDialogue : MonoBehaviour
	{
		private const string DefaultServerAddress = "localhost";
		private const int DefaultServerPort = 7777;
		private const string UserNamePlayerPref = "PlayerName";

		public GameObject accountLoginPanel;
		public GameObject createAccountPanel;
		public GameObject pendingCreationPanel;
		public GameObject informationPanel;
		public GameObject wrongVersionPanel;
		public GameObject controlInformationPanel;
		public GameObject loggingInPanel;
		public GameObject connectionPanel;

		//Account Creation screen:
		public InputField chosenUsernameInput;
		public InputField chosenPasswordInput;
		public InputField emailAddressInput;
		public GameObject goBackCreationButton;
		public GameObject nextCreationButton;

		//Account login:
		public GameObject loginNextButton;
		public GameObject loginGoBackButton;

		public InputField serverAddressInput;
		public InputField serverPortInput;
		public Text dialogueTitle;
		public Text pleaseWaitCreationText;
		public Text loggingInText;
		public Toggle hostServerToggle;
		public Toggle autoLoginToggle;

		private CustomNetworkManager networkManager;

		// Lifecycle
		void Start()
		{
			networkManager = CustomNetworkManager.Instance;

			// Init server address and port defaults
			if (BuildPreferences.isForRelease)
			{
				serverAddressInput.text = Managers.instance.serverIP;
			}
			else
			{
				serverAddressInput.text = DefaultServerAddress;
			}
			serverPortInput.text = DefaultServerPort.ToString();

			OnHostToggle();

			// Init Lobby UI
			InitPlayerName();
			
			//TODO: Enable auto login. If CharacterSettings have not been downloaded for this instance
			// then you need to download them if the user is already logged in. Show a logging in status text
			// when doing this
			// if (ServerData.Auth.CurrentUser != null)
			// {
			// 	ShowConnectionPanel();
			// }
			// else
			// {

			//if (ServerData.Auth?.CurrentUser != null)
			//{
			//	ShowConnectionPanel();
			//}
			//else
			//{

			//	ShowLoginScreen();
			//}
		}

		public void ShowLoginScreen()
		{
			HideAllPanels();
			accountLoginPanel.SetActive(true);
			dialogueTitle.text = "Account Login";
		}

		public void ShowCreationPanel()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			createAccountPanel.SetActive(true);
			dialogueTitle.text = "Create an Account";
		}

		public void ShowCharacterEditor()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			LobbyManager.Instance.characterCustomization.gameObject.SetActive(true);
		}

		private void Update() {
			if ( Input.GetKeyDown( KeyCode.F6 ) )
				if ( Input.GetKeyDown( KeyCode.F6 ) && !BuildPreferences.isForRelease )
				{
					//skip login
					HideAllPanels();
					connectionPanel.SetActive(true);
					dialogueTitle.text = "Connection Panel";
					//if there aren't char settings, default
					if (PlayerManager.CurrentCharacterSettings == null)
					{
						PlayerManager.CurrentCharacterSettings = new CharacterSettings();
					}
				}
		}

		public void ShowConnectionPanel()
		{
			HideAllPanels();
			if (ServerData.Auth.CurrentUser != null)
			{
				connectionPanel.SetActive(true);
				dialogueTitle.text = "Connection Panel";

				StartCoroutine(WaitForReloadProfile());
			}
			else
			{
				loggingInPanel.SetActive(true);
				dialogueTitle.text = "Please Wait..";
			}
		}

		//Make sure we have the latest DisplayName from Auth
		IEnumerator WaitForReloadProfile()
		{
			ServerData.ReloadProfile();

			float timeOutLimit = 60f;
			float timeOutCount = 0f;
			while (string.IsNullOrEmpty(ServerData.Auth.CurrentUser.DisplayName))
			{
				timeOutCount += Time.deltaTime;
				if (timeOutCount >= timeOutLimit)
				{
					Logger.LogError("Failed to load users profile data", Category.DatabaseAPI);
					break;
				}
				yield return WaitFor.EndOfFrame;
			}

			if (!string.IsNullOrEmpty(ServerData.Auth.CurrentUser.DisplayName))
			{
				dialogueTitle.text = "Logged in: " + ServerData.Auth.CurrentUser.DisplayName;
			}
		}

		public void CreationNextButton()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			pendingCreationPanel.SetActive(true);
			nextCreationButton.SetActive(false);
			goBackCreationButton.SetActive(false);
			pleaseWaitCreationText.text = "Please wait..";

			ServerData.TryCreateAccount(chosenUsernameInput.text, chosenPasswordInput.text,
				emailAddressInput.text, AccountCreationSuccess, AccountCreationError);
		}

		private void AccountCreationSuccess(CharacterSettings charSettings)
		{
			pleaseWaitCreationText.text = "Created Successfully";
			PlayerManager.CurrentCharacterSettings = charSettings;
			GameData.LoggedInUsername = chosenUsernameInput.text;
			chosenPasswordInput.text = "";
			chosenUsernameInput.text = "";

			ShowCharacterEditor();
			PlayerPrefs.SetString("lastLogin", emailAddressInput.text);
			PlayerPrefs.Save();
			LobbyManager.Instance.accountLogin.userNameInput.text = emailAddressInput.text;
			emailAddressInput.text = "";
		}

		private void AccountCreationError(string errorText)
		{
			pleaseWaitCreationText.text = errorText;
			goBackCreationButton.SetActive(true);
		}

		public void OnLogin()
		{
			SoundManager.Play("Click01");
			PerformLogin();
		}

		public void PerformLogin()
		{
			if (!LobbyManager.Instance.accountLogin.ValidLogin())
			{
				return;
			}

			HideAllPanels();
			loggingInPanel.SetActive(true);
			loggingInText.text = "Logging in..";
			loginNextButton.SetActive(false);
			loginGoBackButton.SetActive(false);

			LobbyManager.Instance.accountLogin.TryLogin(LoginSuccess, LoginError);
		}

		public void OnLogout()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			ServerData.Auth.SignOut();
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
			PlayerPrefs.Save();
			ShowLoginScreen();
		}

		private void LoginSuccess(string msg)
		{
			loggingInText.text = "Login Success..";
			var characterSettings = JsonUtility.FromJson<CharacterSettings>(Regex.Unescape(msg));
			PlayerPrefs.SetString("currentcharacter", msg);
			PlayerManager.CurrentCharacterSettings = characterSettings;
			ShowConnectionPanel();
		}

		private void LoginError(string msg)
		{
			ServerData.Auth.SignOut(); //just incase
			loggingInText.text = "Login failed:" + msg;
			loginGoBackButton.SetActive(true);
		}

		public void OnHostToggle()
		{
			serverAddressInput.interactable = !hostServerToggle.isOn;
			serverPortInput.interactable = !hostServerToggle.isOn;
		}

		// Button handlers
		public void OnStartGame()
		{
			SoundManager.Play("Click01");

			if (!connectionPanel.activeInHierarchy)
			{
				return;
			}

			// Return if no network address is specified
			if (string.IsNullOrEmpty(serverAddressInput.text))
			{
				return;
			}

			// Set and cache player name
			PlayerPrefs.SetString(UserNamePlayerPref, PlayerManager.CurrentCharacterSettings.Name);

			// Start game
			dialogueTitle.text = "Starting Game...";
			if (BuildPreferences.isForRelease || !hostServerToggle.isOn)
			{
				ConnectToServer();
			}
			else
			{
				networkManager.StartHost();
			}

			// Hide dialogue and show status text
			gameObject.SetActive(false);
			//	UIManager.Chat.CurrentChannelText.text = "<color=green>Loading game please wait..</color>\r\n";
		}

		public void OnShowInformationPanel()
		{
			SoundManager.Play("Click01");
			ShowInformationPanel();
		}

		public void OnShowControlInformationPanel()
		{
			SoundManager.Play("Click01");
			ShowControlInformationPanel();
		}

		public void OnCharacterButton()
		{
			ShowCharacterEditor();
		}

		// Game handlers
		void ConnectToServer()
		{
			// Set network address
			string serverAddress = serverAddressInput.text;
			if (string.IsNullOrEmpty(serverAddress))
			{
				if (BuildPreferences.isForRelease)
				{
					serverAddress = Managers.instance.serverIP;
				}
				if (string.IsNullOrEmpty(serverAddress))
				{
					serverAddress = DefaultServerAddress;
				}
			}

			// Set network port
			int serverPort = 0;
			if (serverPortInput.text.Length >= 4)
			{
				int.TryParse(serverPortInput.text, out serverPort);
			}
			if (serverPort == 0)
			{
				serverPort = DefaultServerPort;
			}

			// Init network client
			networkManager.networkAddress = serverAddress;
			networkManager.networkPort = serverPort;
			networkManager.StartClient();
		}

		void InitPlayerName()
		{
			string steamName = "";
			string prefsName;

			if (Client.Instance != null)
			{
				steamName = Client.Instance.Username;
			}

			if (!string.IsNullOrEmpty(steamName))
			{
				prefsName = steamName;
			}
			else
			{
				prefsName = PlayerPrefs.GetString(UserNamePlayerPref);
			}

			if (!string.IsNullOrEmpty(prefsName))
			{
				//FIXME
				//	playerNameInput.text = prefsName;
			}
		}

		void ShowInformationPanel()
		{
			HideAllPanels();
			informationPanel.SetActive(true);
		}

		void ShowControlInformationPanel()
		{
			HideAllPanels();
			controlInformationPanel.SetActive(true);
		}

		void ShowWrongVersionPanel()
		{
			HideAllPanels();
			wrongVersionPanel.SetActive(true);
		}

		void HideAllPanels()
		{
			//FIXME
			//	startGamePanel.SetActive(false);
			accountLoginPanel.SetActive(false);
			createAccountPanel.SetActive(false);
			pendingCreationPanel.SetActive(false);
			informationPanel.SetActive(false);
			wrongVersionPanel.SetActive(false);
			controlInformationPanel.SetActive(false);
			loggingInPanel.SetActive(false);
			connectionPanel.SetActive(false);
		}
	}
}
