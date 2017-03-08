using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;
using PlayGroup;
using UnityEngine.SceneManagement;

namespace Network {
    public class NetworkManager: Photon.PunBehaviour {

        public PhotonLogLevel logLevel;
        public byte maxPlayersOnServer = 32;
        private bool isConnected = false;

        //Client version number
        public string _gameVersion = "1";

        private static NetworkManager networkManager;

		public GameObject startGameBtn;

        public static NetworkManager Instance {
            get {
                if(!networkManager) {
                    networkManager = FindObjectOfType<NetworkManager>();
                }

                return networkManager;
            }
        }

        void Start() {
            PhotonNetwork.offlineMode = Managers.instance.IsDevMode;
			PhotonView phView = GetComponent<PhotonView>();
			phView.viewID = 99001;
            PhotonNetwork.logLevel = logLevel;
            //no lobby, just server(room)
            PhotonNetwork.autoJoinLobby = false;
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
            PhotonNetwork.automaticallySyncScene = true;
        }

        public static bool IsConnected {
            get {
                return Instance.isConnected;
            }
        }

        public static void Connect() { //Called from login window

            // we check if we are connected or not, we join if we are , else we initiate the connection to the server.
            if(PhotonNetwork.connected) {
                // #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnPhotonRandomJoinFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom(); //When you are done in dev then change this to: PhotonNetwork.JoinRandomRoom();
                Debug.Log("JOIN RANDOM ROOM");
            } else {
                // #Critical, we must first and foremost connect to Photon Online Server.
                PhotonNetwork.ConnectUsingSettings(Instance._gameVersion);
                Debug.Log("CONNECT TO THE PUNderdome");
            }

            UIManager.Display.logInWindow.SetActive(false);
        }

        //Network public functions

        public static void LeaveMap() {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadSceneAsync("Lobby");
        }

        public static void LoadMap() {
            if(!PhotonNetwork.isMasterClient) {
                Debug.Log("You are not the master client, joining map");
                SceneManager.LoadSceneAsync("BoxStation");
            } else {
                Debug.Log("You are the master client, loading the level (default kitchen_construct)");
                SceneManager.LoadSceneAsync("BoxStation");
            }

            UIManager.Display.logInWindow.SetActive(false);
        }

        //PUN CALLBACKS BELOW:

        public override void OnConnectedToMaster() {
            Debug.Log("Connect to PUNderdome");
            UIManager.Chat.ReportToChannel("Server: connecting to server...");
            PhotonNetwork.playerName = UIManager.Chat.UserName;
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnDisconnectedFromPhoton() {
            Debug.Log("DISCONNECTED");
            UIManager.Chat.ReportToChannel("Server: disconnected.");
            isConnected = false;
        }

        public override void OnPhotonRandomJoinFailed(object[] codeAndMsg) {
            Debug.Log("Room Join Failed, creating our own server to be loners on");

            PhotonNetwork.CreateRoom(null, new RoomOptions() { maxPlayers = maxPlayersOnServer }, null); //Create the room with default settings and 32 max players
			if (!GameData.IsInGame) {
				startGameBtn.SetActive(true);
			}
        }

        public override void OnJoinedRoom() {
            Debug.Log("Successfully joined!");

            UIManager.Chat.ReportToChannel("Welcome to unitystation. Press T to chat");
            isConnected = true;
			if (!GameData.IsInGame) {
				if (!PhotonNetwork.isMasterClient) {
					UIManager.Chat.ReportToChannel("Connecting to game....");
				}
			}
            
            PlayerManager.CheckIfSpawned(); // Spawn the character if in the game already (This is for development when you are working on the map scenes)
        }

        public override void OnPhotonPlayerDisconnected(PhotonPlayer other) {
            Debug.Log("PUNderDomePlayerDisconnected() " + other.NickName); // seen when other disconnects
			PlayerList.Instance.RemovePlayer(other.NickName);

		}

        public override void OnPhotonPlayerConnected(PhotonPlayer other) {
            Debug.Log("OnPhotonPlayerConnected() " + other.NickName); // not seen if you're the player connecting
		
		}
    }
}