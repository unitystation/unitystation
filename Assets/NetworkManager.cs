using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;

namespace Network
{

	public class NetworkManager : Photon.PunBehaviour {

		public PhotonLogLevel logLevel;
		public byte maxPlayersOnServer = 32;
		public bool isConnected = false;

		//Client version number
		string _gameVersion = "1";

	
	void Awake(){

			PhotonNetwork.logLevel = logLevel;
			//no lobby, just server(room)
			PhotonNetwork.autoJoinLobby = false;
			// this makes sure we can use PhotonNetwork.LoadLevel() on the master client and all clients in the same room sync their level automatically
			PhotonNetwork.automaticallySyncScene = true;

	}

	void Start () {
		
	
	}
	
	// Update is called once per frame
	void Update () {
		
	}

		public void Connect(){ //Called from login window

			// we check if we are connected or not, we join if we are , else we initiate the connection to the server.
			if (PhotonNetwork.connected)
			{
				// #Critical we need at this point to attempt joining a Random Room. If it fails, we'll get notified in OnPhotonRandomJoinFailed() and we'll create one.
				PhotonNetwork.JoinRandomRoom();
			}else{
				// #Critical, we must first and foremost connect to Photon Online Server.
				PhotonNetwork.ConnectUsingSettings(_gameVersion);
			}


		}

		//PUN CALLBACKS BELOW:

		public override void OnConnectedToMaster ()
		{
			Debug.Log ("ON CONNECTED CALLED ON NETWORKMANAGER");
			PhotonNetwork.playerName = UIManager.control.chatControl.UserName;
			PhotonNetwork.JoinRandomRoom ();
		} 

		public override void OnDisconnectedFromPhoton ()
		{
			Debug.Log ("DISCONNECTED FROM PHOTON");
			isConnected = false;
		}

		public override void OnPhotonRandomJoinFailed (object[] codeAndMsg)
		{
			Debug.Log ("NO RANDOM ROOM FOUND, LETS CREATE ONE");

			PhotonNetwork.CreateRoom (null, new RoomOptions () { maxPlayers = maxPlayersOnServer }, null); //Create the room with default settings and 32 max players
		}

		public override void OnJoinedRoom ()
		{
			Debug.Log ("CLIENT IS NOW IN THE ROOM(SERVER)");
			isConnected = true;
			Managers.control.playerManager.CheckIfSpawned (); // Spawn the character if in the game already (This is for development when you are working on the map scenes)
		}
}
}