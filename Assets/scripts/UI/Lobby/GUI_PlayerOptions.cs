using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using Items;

namespace UI
{
	public class GUI_PlayerOptions: MonoBehaviour
	{
		public Text title;

		public InputField playerNameInput;
		public InputField serverAddressInput;
		public InputField portInput;

		public Toggle hostServer;
        private CustomNetworkManager networkManager;
		public GameObject screen_PlayerName;
		public GameObject screen_ConnectTo;

		private const string UserNamePlayerPref = "PlayerName";

		private const string DefaultServer = "LocalHost";
		private const string DefaultPort = "7777";

		public void Start()
		{
            networkManager = CustomNetworkManager.Instance;
			screen_PlayerName.SetActive(true);
			screen_ConnectTo.SetActive(false);
			string prefsName = PlayerPrefs.GetString(GUI_PlayerOptions.UserNamePlayerPref);
			if (!string.IsNullOrEmpty(prefsName)) {
				playerNameInput.text = prefsName;
			}
			serverAddressInput.text = DefaultServer;
			portInput.text = DefaultPort;
		}

		public void EndEditOnEnter()
		{
			if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) {
				BtnOk();
			}
		}

		public void BtnOk()
		{
			SoundManager.Play("Click01");

			//Connecting as client
			if (screen_ConnectTo.activeInHierarchy) {
				ConnectToServer();
				gameObject.SetActive(false);
			}	
				
			if (screen_PlayerName.activeInHierarchy && !hostServer.isOn) {
				PlayerPrefs.SetString(GUI_PlayerOptions.UserNamePlayerPref, playerNameInput.text);
				screen_PlayerName.SetActive(false);
				screen_ConnectTo.SetActive(true);
				title.text = "Connection";
			}

			//Connecting as server from a map scene
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && GameData.IsInGame) {
				PlayerPrefs.SetString(GUI_PlayerOptions.UserNamePlayerPref, playerNameInput.text);
                networkManager.StartHost();
				gameObject.SetActive(false);
			}

			//Connecting as server from the lobby
			if (screen_PlayerName.activeInHierarchy && hostServer.isOn && !GameData.IsInGame) {
				PlayerPrefs.SetString(GUI_PlayerOptions.UserNamePlayerPref, playerNameInput.text);
                networkManager.StartHost();
				gameObject.SetActive(false);
			}
		}

		void ConnectToServer()
		{
            networkManager.networkAddress = serverAddressInput.text;
			int port = 0;
			if (portInput.text.Length == 3) {
				int.TryParse(portInput.text, out port);
			}
			if (port == 0) {
                networkManager.networkPort = 7777;
			} else {
                networkManager.networkPort = port;
			}
            networkManager.StartClient();
		}
	}
}
