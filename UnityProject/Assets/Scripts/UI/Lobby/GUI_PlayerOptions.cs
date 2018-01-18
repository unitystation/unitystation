using PlayGroup;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;


namespace UI
{
	public class GUI_PlayerOptions : MonoBehaviour
	{
		private const string UserNamePlayerPref = "PlayerName";

		private const string DefaultServer = "LocalHost";
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
			string prefsName;
			if(SteamManager.Initialized) {
				steamName = SteamFriends.GetPersonaName();
			}
			if (steamName != "" || steamName == null)
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
			if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
			{
				BtnOk();
			}
		}

		public void BtnOk()
		{
			SoundManager.Play("Click01");
			if (string.IsNullOrEmpty(playerNameInput.text.Trim()))
			{
				return;
			}

			//Connecting as client
			if (screen_ConnectTo.activeInHierarchy || Managers.instance.isForRelease)
			{
				PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
				PlayerManager.PlayerNameCache = playerNameInput.text;
				ConnectToServer();
				gameObject.SetActive(false);
				UIManager.Chat.CurrentChannelText.text = "<color=green>Loading game please wait..</color>\r\n";
				return;
			}

			if (screen_PlayerName.activeInHierarchy && !hostServer.isOn)
			{
				PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
				PlayerManager.PlayerNameCache = playerNameInput.text;
				screen_PlayerName.SetActive(false);
				screen_ConnectTo.SetActive(true);
				title.text = "Connection";
			}

			//Connecting as server from a map scene
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && GameData.IsInGame)
			{
				PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
				PlayerManager.PlayerNameCache = playerNameInput.text;
				networkManager.StartHost();
				gameObject.SetActive(false);
			}

			//Connecting as server from the lobby
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && !GameData.IsInGame)
			{
				PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
				PlayerManager.PlayerNameCache = playerNameInput.text;
				networkManager.StartHost();
				gameObject.SetActive(false);
			}
		}

		private void ConnectToServer()
		{
			if (Managers.instance.isForRelease)
			{
				networkManager.networkAddress = Managers.instance.serverIP;
				networkManager.networkPort = 7777;
				networkManager.StartClient();
				return;
			}

			networkManager.networkAddress = serverAddressInput.text;
			int port = 0;
			if (portInput.text.Length >= 4)
			{
				int.TryParse(portInput.text, out port);
			}
			if (port == 0)
			{
				networkManager.networkPort = 7777;
			}
			else
			{
				networkManager.networkPort = port;
			}
			networkManager.StartClient();
		}
	}
}