using Facepunch.Steamworks;
using UnityEngine;
using UnityEngine.UI;



	public class GUI_LobbyDialogue : MonoBehaviour
	{

		private const string DefaultServerAddress = "localhost";
		private const int DefaultServerPort = 7777;
		private const string UserNamePlayerPref = "PlayerName";

		public GameObject startGamePanel;
		public GameObject informationPanel;
		public GameObject wrongVersionPanel;
		public GameObject controlInformationPanel;

		public InputField playerNameInput;
		public InputField serverAddressInput;
		public InputField serverPortInput;
		public Text dialogueTitle;
		public Toggle hostServerToggle;

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

			// OnChange handler for toggle to
			// disable server address and port
			// input fields
			hostServerToggle.onValueChanged.AddListener(isOn =>
				{
					serverAddressInput.interactable = !isOn;
					serverPortInput.interactable = !isOn;
				}
			);
			hostServerToggle.onValueChanged.Invoke( hostServerToggle.isOn );

			// Init Lobby UI
			InitPlayerName();
			ShowStartGamePanel();
		}

		// Button handlers
		public void OnStartGame()
		{
			SoundManager.Play("Click01");

			// Return if no player name or incorrect screen
			if (string.IsNullOrEmpty(playerNameInput.text.Trim()))
			{
				return;
			}
			if (!startGamePanel.activeInHierarchy)
			{
				return;
			}

			// Return if no network address is specified
			if (string.IsNullOrEmpty(serverAddressInput.text))
			{
				return;
			}

			// Set and cache player name
			PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
			PlayerManager.PlayerNameCache = playerNameInput.text;

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

		public void OnTryAgain()
		{
			SoundManager.Play("Click01");
			ShowStartGamePanel();
		}

		public void OnReturnToPlayerLogin()
		{
			SoundManager.Play("Click01");
			ShowStartGamePanel();
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
				playerNameInput.text = prefsName;
			}
		}

		// Panel helpers
		void ShowStartGamePanel()
		{
			HideAllPanels();
			startGamePanel.SetActive(true);
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
			startGamePanel.SetActive(false);
			informationPanel.SetActive(false);
			wrongVersionPanel.SetActive(false);
			controlInformationPanel.SetActive(false);
		}
	}

