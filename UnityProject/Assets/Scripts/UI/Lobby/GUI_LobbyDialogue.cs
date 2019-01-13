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

			//TODO TODO: Check if Auto login is set and if both username and password are saved
			ShowLoginScreen();
		}

		private void Update() {
			if ( Input.GetKeyDown( KeyCode.F6 ) && !BuildPreferences.isForRelease )
			{
				GameData.IsLoggedIn = true;
				ShowCharacterEditor();
			}
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

		public void ShowConnectionPanel()
		{
			HideAllPanels();
			if (GameData.IsLoggedIn)
			{
				connectionPanel.SetActive(true);
				dialogueTitle.text = "Logged in as: " + GameData.LoggedInUsername;
			}
			else
			{
				loggingInPanel.SetActive(true);
				dialogueTitle.text = "Please Wait..";
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

		private void AccountCreationSuccess(string message)
		{
			pleaseWaitCreationText.text = "Created Successfully";
			PlayerManager.CurrentCharacterSettings = new CharacterSettings();
			GameData.LoggedInUsername = chosenUsernameInput.text;
			GameData.IsLoggedIn = true;
			chosenPasswordInput.text = "";
			chosenUsernameInput.text = "";
			emailAddressInput.text = "";
			//	nextCreationButton.SetActive(true);
		}

		private void AccountCreationError(string errorText)
		{
			pleaseWaitCreationText.text = errorText;
			goBackCreationButton.SetActive(true);
		}

		public void OnLogin()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			loggingInPanel.SetActive(true);
			loggingInText.text = "Logging in..";
			loginNextButton.SetActive(false);
			loginGoBackButton.SetActive(false);

			LobbyManager.Instance.accountLogin.TryLogin(LoginSuccess, LoginError, autoLoginToggle.isOn);
		}

		public void OnLogout()
		{
			SoundManager.Play("Click01");
			HideAllPanels();
			GameData.IsLoggedIn = false;
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
			PlayerPrefs.Save();
		}

		private void LoginSuccess(string msg)
		{
			loggingInText.text = "Login Success..";
			var characterSettings = JsonUtility.FromJson<CharacterSettings>(Regex.Unescape(msg));
			PlayerPrefs.SetString("currentcharacter", msg);
			PlayerManager.CurrentCharacterSettings = characterSettings;
		}

		private void LoginError(string msg)
		{
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