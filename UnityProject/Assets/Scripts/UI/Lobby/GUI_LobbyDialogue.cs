using Facepunch.Steamworks;
using PlayGroup;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    public class GUI_LobbyDialogue : MonoBehaviour
    {

        private const string DefaultServer = "localhost";
        private const string DefaultPort = "7777";
        private const string UserNamePlayerPref = "PlayerName";

        public GameObject startGamePanel;
        public GameObject informationPanel;
        public GameObject wrongVersionPanel;

        public InputField playerNameInput;
        public InputField serverAddressInput;
        public InputField serverPortInput;
        public Text dialogueTitle;
        public Toggle hostServerToggle;

        private CustomNetworkManager networkManager;

        // Lifecycle
        void Start () {
	        networkManager = CustomNetworkManager.Instance;
            serverAddressInput.text = DefaultServer;
            serverPortInput.text = DefaultPort;

            InitPlayerName();

            ShowStartGamePanel();
        }

        void Update () {
            serverAddressInput.interactable = !hostServerToggle.isOn;
            serverPortInput.interactable = !hostServerToggle.isOn;
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

            // Set and cache player name
            PlayerPrefs.SetString(UserNamePlayerPref, playerNameInput.text);
            PlayerManager.PlayerNameCache = playerNameInput.text;

            // Start game
            dialogueTitle.text = "Starting Game...";
            if (Managers.instance.isForRelease || !hostServerToggle.isOn)
            {
                ConnectToServer();
            }
            else
            {
                networkManager.StartHost();
            }

            // Hide dialogue and show status text
            gameObject.SetActive(false);
            UIManager.Chat.CurrentChannelText.text = "<color=green>Loading game please wait..</color>\r\n";
        }

        public void OnShowInformationPanel()
        {
            SoundManager.Play("Click01");
            ShowInformationPanel();
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
        void StartSelfHostedGame()
        {
            gameObject.SetActive(false);
        }

        void ConnectToServer()
        {
            if (Managers.instance.isForRelease)
            {
                networkManager.networkAddress = Managers.instance.serverIP;
                networkManager.networkPort = int.Parse(DefaultPort);
                networkManager.StartClient();
                return;
            }

            networkManager.networkAddress = serverAddressInput.text;
            int port = 0;
            if (serverPortInput.text.Length >= 4)
            {
                int.TryParse(serverPortInput.text, out port);
            }
            if (port == 0)
            {
                networkManager.networkPort = int.Parse(DefaultPort);
            }
            else
            {
                networkManager.networkPort = port;
            }
            networkManager.StartClient();
        }

        // Player name helpers
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
            startGamePanel.SetActive(true);
            HideInformationPanel();
            HideWrongVersionPanel();
        }

        void HideStartGamePanel()
        {
            startGamePanel.SetActive(false);
        }

        void ShowInformationPanel()
        {
            informationPanel.SetActive(true);
            HideStartGamePanel();
            HideWrongVersionPanel();
        }

        void HideInformationPanel()
        {
            informationPanel.SetActive(false);
        }

        void ShowWrongVersionPanel()
        {
            wrongVersionPanel.SetActive(true);
            HideStartGamePanel();
            HideInformationPanel();
        }

        void HideWrongVersionPanel()
        {
            wrongVersionPanel.SetActive(false);
        }
    }
}
