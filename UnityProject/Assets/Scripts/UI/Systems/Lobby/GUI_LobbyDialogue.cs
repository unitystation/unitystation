using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Firebase.Auth;
using IgnoranceTransport;
using Newtonsoft.Json;
using System.Linq;


namespace Lobby
{
	public class GUI_LobbyDialogue :  MonoBehaviour
	{
		public static GUI_LobbyDialogue Instance;
		private const string DefaultServerAddress = "127.0.0.1";
		private const ushort DefaultServerPort = 7777;
		private const string UserNamePlayerPref = "PlayerName";

		public GameObject mainPanel;
		public GameObject joinPanel;
		public GameObject accountLoginPanel;
		public GameObject createAccountPanel;
		public GameObject pendingCreationPanel;
		public GameObject informationPanel;
		public GameObject wrongVersionPanel;
		public GameObject controlInformationPanel;
		public GameObject loggingInPanel;
		public GameObject disconnectPanel;

		//Account Creation screen:
		public InputField chosenUsernameInput;
		public InputField chosenPasswordInput;
		public InputField emailAddressInput;
		public GameObject goBackCreationButton;
		public GameObject nextCreationButton;

		//Account login:
		public GameObject loginNextButton;
		public GameObject loginGoBackButton;
		public Button resendEmailButton;

		public InputField serverAddressInput;
		public InputField serverPortInput;
		public Text dialogueTitle;
		public Text menuUsernameText;
		public Text serverConnectionFailedText;
		public Text pleaseWaitCreationText;
		public Text loggingInText;
		public Toggle autoLoginToggle;

		public bool wasDisconnected = false;

		private List<ConnectionHistory> history = new();
		private string isWindows = "/";
		private string historyFilePath;
		[SerializeField] private GameObject historyLogEntryGameObject;
		[SerializeField] private GameObject historyEntries;
		[SerializeField] private GameObject historyPanel;
		[SerializeField] private GameObject logShowButton;
		[SerializeField] private int entrySizeLimit = 5;

		private GameObject[] allPanels;

		#region Lifecycle

		private void Awake()
		{
			allPanels = new GameObject[] { mainPanel, joinPanel, accountLoginPanel, createAccountPanel,
					pendingCreationPanel,informationPanel, controlInformationPanel, loggingInPanel, disconnectPanel };

			isWindows = Application.persistentDataPath.Contains("/") ? $"/" : $"\\";
			Instance = this;
			historyFilePath = $"{Application.persistentDataPath}{isWindows}ConnectionHistory.json";
			if (File.Exists(historyFilePath))
			{
				string json = File.ReadAllText(historyFilePath);
				if(json.Length <= 3) return;
				history = JsonConvert.DeserializeObject<List<ConnectionHistory>>(json);
				GenerateHistoryData();
				if(historyEntries.transform.childCount > 0) logShowButton.SetActive(true);
			}
		}

		private void Start()
		{
			// Init Lobby UI
			HideAllPanels();
			InitPlayerName();

			if (ServerData.Auth?.CurrentUser == null)
			{
				ShowLoginScreen();
			}
			else if (wasDisconnected && GameManager.Instance.DisconnectExpected == false)
			{
				ShowDisconnectPanel();
			}
			else
			{
				ShowMainPanel();
			}

			// reset
			wasDisconnected = false;
			GameManager.Instance.DisconnectExpected = false;
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
				//if there aren't char settings, default
				if (PlayerManager.CurrentCharacterSheet == null)
				{
					PlayerManager.CurrentCharacterSheet = new CharacterSheet();
				}
			}
		}

		private void GenerateHistoryData()
		{
			if(history.Count == 0) return;
			var numberOfGeneratedEntries = 0;
			foreach (var historyEntry in history)
			{
				if(numberOfGeneratedEntries >= entrySizeLimit) break;
				if(historyEntry.IP == DefaultServerAddress) continue;
				var newEntry = Instantiate(historyLogEntryGameObject, historyEntries.transform);
				newEntry.GetComponent<HistoryLogEntry>().SetData(historyEntry.IP, numberOfGeneratedEntries);
				newEntry.SetActive(true);
				numberOfGeneratedEntries++;
			}
		}

		#endregion

		public void OnClientDisconnect()
		{
			LoadingScreenManager.Instance.CloseLoadingScreen();
			gameObject.SetActive(true);
			ShowJoinPanel();
			StartCoroutine(FlashConnectionFailedText());
		}

		private IEnumerator FlashConnectionFailedText()
		{
			serverConnectionFailedText.gameObject.SetActive(true);
			yield return WaitFor.Seconds(5);
			serverConnectionFailedText.gameObject.SetActive(false);
		}

		public void ShowMainPanel()
		{
			HideAllPanels();
			mainPanel.SetActive(true);
			menuUsernameText.text = $"Logged in as {ServerData.Auth.CurrentUser.DisplayName}";
			dialogueTitle.text = string.Empty;
		}

		public void ShowLoginScreen()
		{
			HideAllPanels();
			accountLoginPanel.SetActive(true);
			dialogueTitle.text = "Account Login";
		}

		public void ShowCreationPanel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			HideAllPanels();
			createAccountPanel.SetActive(true);
			dialogueTitle.text = "Create an Account";
		}

		public void ShowCharacterEditor(Action onCloseAction = null)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			LobbyManager.Instance.characterCustomization.gameObject.SetActive(true);
			if (onCloseAction != null)
			{
				LobbyManager.Instance.characterCustomization.onCloseAction = onCloseAction;
			}
		}

		public void ShowJoinPanel()
		{
			HideAllPanels();
			joinPanel.SetActive(true);

			if (history.Count > 0) {
				serverAddressInput.text = history.Last().IP;
				serverPortInput.text = history.Last().Port.ToString();
			}

			dialogueTitle.text = "Join Game";
		}

		public void ShowDisconnectPanel()
		{
			HideAllPanels();
			disconnectPanel.SetActive(true);
			dialogueTitle.text = "Disconnected";
		}

		//Make sure we have the latest DisplayName from Auth
		private IEnumerator WaitForReloadProfile()
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

			menuUsernameText.text = $"Logged in as {ServerData.Auth.CurrentUser.DisplayName}";
		}

		public void CreationNextButton()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			HideAllPanels();
			pendingCreationPanel.SetActive(true);
			nextCreationButton.SetActive(false);
			goBackCreationButton.SetActive(false);
			pleaseWaitCreationText.text = "Please wait..";

			ServerData.TryCreateAccount(chosenUsernameInput.text, chosenPasswordInput.text,
				emailAddressInput.text, AccountCreationSuccess, AccountCreationError);
		}

		private void AccountCreationSuccess(CharacterSheet charSettings)
		{
			pleaseWaitCreationText.text = $"Success! An email has been sent to {emailAddressInput.text}. " +
										  $"Please click the link in the email to verify " +
										  $"your account before signing in.";
			PlayerManager.CurrentCharacterSheet = charSettings;
			GameData.LoggedInUsername = chosenUsernameInput.text;
			chosenPasswordInput.text = "";
			chosenUsernameInput.text = "";
			goBackCreationButton.SetActive(true);
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
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			PerformLogin();
		}

		public void PerformLogin()
		{
			if (!LobbyManager.Instance.accountLogin.ValidLogin())
			{
				return;
			}

			ShowLoggingInStatus("Logging in..");
			LobbyManager.Instance.accountLogin.TryLogin(LoginSuccess, LoginError);
		}

		public void ShowLoggingInStatus(string status)
		{
			HideAllPanels();

			loggingInPanel.SetActive(true);
			loggingInText.text = status;
			loginNextButton.SetActive(false);
			loginGoBackButton.SetActive(false);
		}

		public void OnExit()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			Application.Quit();
		}

		public void LoginSuccess(string msg)
		{
			loggingInText.text = "Login Success";
			ShowMainPanel();
		}

		public void LoginError(string msg)
		{
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
		}

		public void OnEmailResend()
		{
			resendEmailButton.interactable = false;
			loggingInText.text =
				$"A new verification email has been sent to {FirebaseAuth.DefaultInstance.CurrentUser.Email}.";
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			FirebaseAuth.DefaultInstance.CurrentUser.SendEmailVerificationAsync();
			FirebaseAuth.DefaultInstance.SignOut();
		}

		#region Button handlers

		public void OnMainMenuJoinBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ShowJoinPanel();
		}

		public void OnMainMenuHostBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			dialogueTitle.text = "Hosting Game...";

			// Set and cache player name
			PlayerPrefs.SetString(UserNamePlayerPref, PlayerManager.CurrentCharacterSheet.Name);

			LoadingScreenManager.LoadFromLobby(CustomNetworkManager.Instance.StartHost);
		}

		public void OnJoinMenuJoinBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			// Return if no network address is specified
			if (string.IsNullOrEmpty(serverAddressInput.text)) return;

			dialogueTitle.text = "Joining Game...";

			ConnectToServer();
		}

		public void OnStartGameFromHub()
		{
			if (PlayerManager.CurrentCharacterSheet != null)
			{
				PlayerPrefs.SetString(UserNamePlayerPref, PlayerManager.CurrentCharacterSheet.Name);
			}
			
			ConnectToServer();
			dialogueTitle.text = "Joining Game...";
			ShowLoggingInStatus("Joining the game...");
		}

		public void OnShowInformationPanel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ShowInformationPanel();
		}

		public void OnShowControlInformationPanel()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			ShowControlInformationPanel();
		}

		public void OnCharacterButton()
		{
			ShowCharacterEditor(OnCharacterExit);
		}

		private void OnCharacterExit()
		{
			gameObject.SetActive(true);
			if (ServerData.Auth.CurrentUser != null)
			{
				ShowJoinPanel();
			}
			else
			{
				Logger.LogWarning("User is not logged in! Returning to login screen.", Category.Connections);
				ShowLoginScreen();
			}
		}

		public void OnLogoutBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			HideAllPanels();
			ServerData.Auth.SignOut();
			NetworkClient.Disconnect();
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
			PlayerPrefs.Save();
			ShowLoginScreen();
		}

		#endregion

		// Game handlers
		public void ConnectToServer()
		{
			LoadingScreenManager.LoadFromLobby(DoServerConnect);
		}

		private void DoServerConnect()
		{
			// Set network address
			string serverAddress = serverAddressInput.text;
			if (string.IsNullOrEmpty(serverAddress))
			{
				serverAddress = DefaultServerAddress;
			}

			// Set network port
			ushort serverPort = DefaultServerPort;
			if (serverPortInput.text.Length >= 4)
			{
				ushort.TryParse(serverPortInput.text, out serverPort);
			}

			// Init network client
			Logger.LogFormat("Client trying to connect to {0}:{1}", Category.Connections, serverAddress, serverPort);

			CustomNetworkManager.Instance.networkAddress = serverAddress;

			var telepathy = CustomNetworkManager.Instance.GetComponent<TelepathyTransport>();
			if (telepathy != null)
			{
				telepathy.port = serverPort;
			}

			var ignorance = CustomNetworkManager.Instance.GetComponent<Ignorance>();
			if (ignorance != null)
			{
				ignorance.port = serverPort;
			}

			// var booster = CustomNetworkManager.Instance.GetComponent<BoosterTransport>();
			// if (booster != null)
			// {
			// 	booster.port = serverPort;
			// }
			LogConnectionToHistory(serverAddress, serverPort);

			CustomNetworkManager.Instance.StartClient();
		}

		private void LogConnectionToHistory(string serverAddress, int serverPort)
		{
			ConnectionHistory newHistoryEntry = new ConnectionHistory();
			newHistoryEntry.IP = serverAddress;
			newHistoryEntry.Port = serverPort;
			history.Add(newHistoryEntry);
			UpdateHistoryFile();
		}

		private void UpdateHistoryFile()
		{
			string json = JsonConvert.SerializeObject(history);
			if(File.Exists(historyFilePath)) File.Delete(historyFilePath);
			while (!File.Exists(historyFilePath))
			{
				var fs = new FileStream(historyFilePath, FileMode.Create); //To avoid share rule violations
				fs.Dispose();
				File.WriteAllText(historyFilePath, json);
			}
		}

		private void InitPlayerName()
		{
			string steamName = "";
			string prefsName;

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

		private void ShowInformationPanel()
		{
			HideAllPanels();
			informationPanel.SetActive(true);
		}

		private void ShowControlInformationPanel()
		{
			HideAllPanels();
			controlInformationPanel.SetActive(true);
		}

		private void ShowWrongVersionPanel()
		{
			HideAllPanels();
			wrongVersionPanel.SetActive(true);
		}

		public void HideAllPanels()
		{
			foreach (var panel in allPanels)
			{
				if (panel != null)
				{
					panel.SetActive(false);
				}
			}
		}

		public void ConnectToServerFromHistory(int historyIndex)
		{
			serverAddressInput.text = history[historyIndex].IP;
			serverPortInput.text = history[historyIndex].Port.ToString();
			DoServerConnect();
		}

		public void ConnectToLastServer()
		{
			ConnectToServerFromHistory(history.Count - 1);
		}

		public void OnShowLogButton()
		{
			historyPanel.SetActive(!historyPanel.activeSelf);
		}

		public struct ConnectionHistory
		{
			public string IP;
			public int Port;
		}
	}
}
