using Managers;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class GUI_PlayerOptions : MonoBehaviour
	{
		private const string UserNamePlayerPref = "PlayerName";

		public string DefaultServer = "LocalHost";
		private const string DefaultPort = "7777";
		public GameObject button;

		public Toggle hostServer;
		private CustomNetworkManager networkManager;

		public InputField playerNameInput;
		public InputField portInput;
		public GameObject screen_ConnectTo;
		public GameObject screen_PlayerName;
		public GameObject screen_WrongVersion;
		public InputField serverAddressInput;
		public Text title;

		public void Start()
		{
			networkManager = CustomNetworkManager.Instance;
			screen_PlayerName.SetActive(true);
			screen_ConnectTo.SetActive(false);
			string steamName = "";
			string prefsName = string.IsNullOrEmpty(steamName) ? PlayerPrefs.GetString(UserNamePlayerPref) : steamName;

			if (string.IsNullOrEmpty(prefsName) == false)
			{
				playerNameInput.text = prefsName;
			}
			serverAddressInput.text = DefaultServer;
			portInput.text = DefaultPort;
		}

		private void WrongVersion()
		{
			screen_PlayerName.SetActive(false);
			screen_ConnectTo.SetActive(false);
			button.SetActive(false);
			screen_WrongVersion.SetActive(true);
		}

		public void EndEditOnEnter()
		{
			if (KeyboardInputManager.IsEnterPressed())
			{
				BtnOk();
			}
		}

		public void BtnOk()
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
			if (string.IsNullOrEmpty(playerNameInput.text.Trim()))
			{
				return;
			}

			// Connecting as client
			if (screen_ConnectTo.activeInHierarchy || BuildPreferences.isForRelease)
			{
				ConnectToServer();
				gameObject.SetActive(false);
				//		UIManager.Chat.CurrentChannelText.text = "<color=green>Loading game please wait..</color>\r\n";
				return;
			}

			if (screen_PlayerName.activeInHierarchy && !hostServer.isOn)
			{
				screen_PlayerName.SetActive(false);
				screen_ConnectTo.SetActive(true);
				title.text = "Connection";
			}

			// Connecting as server from a map scene
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && GameData.IsInGame)
			{
				networkManager.StartHost();
				gameObject.SetActive(false);
			}

			// Connecting as server from the lobby
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && !GameData.IsInGame)
			{
				networkManager.StartHost();
				gameObject.SetActive(false);
			}
		}

		private void ConnectToServer()
		{
			if (BuildPreferences.isForRelease)
			{
				networkManager.networkAddress = GameScreenManager.Instance.serverIP;
				networkManager.StartClient();
				return;
			}

			networkManager.networkAddress = serverAddressInput.text;
			TelepathyTransport transport = CustomNetworkManager.Instance.GetComponent<TelepathyTransport>();
			ushort port = 0;
			if (portInput.text.Length >= 4)
			{
				ushort.TryParse(portInput.text, out port);
			}
			if (port == 0)
			{
				transport.port = 7777;
			}
			else
			{
				transport.port = port;
			}
			networkManager.StartClient();
		}
	}
}
