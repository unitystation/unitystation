using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Newtonsoft.Json;
using System.Linq;


namespace Lobby
{
	public class GUI_LobbyDialogue :  MonoBehaviour
	{
		#region Inspector fields

		[SerializeField]
		private Text dialogueTitle = default;

		// TODO handle
		public GameObject mainPanel;
		public GameObject joinPanel;
		public GameObject accountLoginPanel;
		public GameObject createAccountPanel;
		public GameObject informationPanel;
		public GameObject controlInformationPanel;

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

		// join panel TODO move
		public InputField serverAddressInput;
		public InputField serverPortInput;
		public Text serverConnectionFailedText;

		[SerializeField] private GameObject historyLogEntryGameObject;
		[SerializeField] private GameObject historyEntries;
		[SerializeField] private GameObject historyPanel;
		[SerializeField] private GameObject logShowButton;
		[SerializeField] private int entrySizeLimit = 5;

		#endregion

		private const string DefaultServerAddress = "127.0.0.1";
		private const ushort DefaultServerPort = 7777;

		[NonSerialized]
		public bool wasDisconnected = false;

		public AccountLoginPanel LoginUIScript => accountLoginScript;

		private GameObject[] allPanels;

		private List<ConnectionHistory> history = new();
		private string isWindows = "/";
		private string historyFilePath;

		#region Lifecycle

		private void Awake()
		{
			// TODO sort out new generic panels
			allPanels = new GameObject[]
			{
					accountLoginScript.gameObject, accountCreateScript.gameObject,
					mainPanel, joinPanel, accountLoginPanel, createAccountPanel,
					informationPanel, controlInformationPanel
			};

			isWindows = Application.persistentDataPath.Contains("/") ? $"/" : $"\\";
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

			if (ServerData.Auth?.CurrentUser == null)
			{
				ShowLoginPanel();
			}
			else if (wasDisconnected && GameManager.Instance.DisconnectExpected == false)
			{
				ShowInfoPanel(new InfoPanelArgs
				{
					IsError = true,
					Heading = "Lost Connection",
					Text = "Lost connection to the server. Check your console (F5).",
					LeftButtonText = "Back",
					LeftButtonCallback = ShowMainPanel,
					RightButtonText = "Rejoin",
					RightButtonCallback = ConnectToLastServer,
				});
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
			mainMenuScript.SetSignedInText();
			dialogueTitle.text = string.Empty;
		}

		public void ShowLoginPanel()
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

		public void ShowInfoPanel(InfoPanelArgs args)
		{
			HideAllPanels();
			infoPanelScript.gameObject.SetActive(true);
			infoPanelScript.Show(args);
		}

		public void ShowLoadingPanel(LoadingPanelArgs args)
		{
			HideAllPanels();
			loadingPanelScript.gameObject.SetActive(true);
			loadingPanelScript.Show(args);
		}

		// TODO check usages
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

		#region Button handlers

		// TODO re/move these

		public void OnJoinMenuJoinBtn()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);

			if (string.IsNullOrEmpty(serverAddressInput.text))
			{
				serverAddressInput.text = DefaultServerAddress;
			}

			if (string.IsNullOrEmpty(serverPortInput.text))
			{
				serverPortInput.text = DefaultServerPort.ToString();
			}

			if (ushort.TryParse(serverPortInput.text, out var serverPort) == false)
			{
				Logger.LogError("Cannot join server: invalid port.");
				return;
			}

			LobbyManager.Instance.JoinServer(serverAddressInput.text, serverPort);
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
				ShowLoginPanel();
			}
		}

		#endregion

		public void LogConnectionToHistory(string serverAddress, int serverPort)
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

		private void ShowWrongVersionPanel() // TODO
		{
			HideAllPanels();

			// TODO use ShowInfoPanel()
			//wrongVersionPanel.SetActive(true);
			dialogueTitle.text = "Wrong Version";
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
			LobbyManager.Instance.JoinServer(history[historyIndex].IP, (ushort) history[historyIndex].Port);
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
