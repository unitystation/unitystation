using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UI;
using PlayGroup;
using UnityEngine.SceneManagement;
using Items;

public class CustomNetworkManager: NetworkManager
{
	public static CustomNetworkManager Instance;
	[HideInInspector]
	public bool _isServer = false;
	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(this.gameObject);
		}
	}

	void Start(){
		if (!IsClientConnected() && !GameData.IsHeadlessServer)
		{
			UIManager.Display.logInWindow.SetActive(true);   
		}
	}

	void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	public override void OnStartServer(){
		_isServer = true;
		base.OnStartServer();
	}

	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId){
		//This spawns the player prefab
			base.OnServerAddPlayer(conn, playerControllerId);
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
		if (_isServer) {
		//do special server wizardry here

		}
		//This client connecting to server
		base.OnClientConnect(conn);
	}

	IEnumerator WaitForLoad(NetworkConnection conn, short playerID){
		yield return new WaitForSeconds(2f);
		base.OnServerAddPlayer(conn, playerID);
	}

	public override void OnServerDisconnect(NetworkConnection conn)
	{
		PlayerList.Instance.RemovePlayer(conn.playerControllers[0].gameObject.name);
		//TODO DROP ALL HIS OBJECTS
		Debug.Log("PlayerDisconnected: " + conn.playerControllers[0].gameObject.name);
		NetworkServer.Destroy(conn.playerControllers[0].gameObject);
	}

	void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (IsClientConnected())
		{
			//make sure login window does not show on scene changes if connected
			UIManager.Display.logInWindow.SetActive(false);
			StartCoroutine(DoHeadlessCheck());
		}
		else
		{
			StartCoroutine(DoHeadlessCheck());
		}
	}

	IEnumerator DoHeadlessCheck(){
		yield return new WaitForEndOfFrame();
		if (!GameData.IsHeadlessServer) {
			if(!IsClientConnected())
			UIManager.Display.logInWindow.SetActive(true);
			
		} else {
		    //Set up for headless mode stuff here
			//Useful for turning on and off components

			/*Hacky approach, we are running a Host not a straight server.
              so once the server player is spawned, we will remove him from the scene
              and delete his name from player list
              */
			_isServer = true;
			if(GameData.IsInGame){
				if(PlayerList.Instance != null)
			PlayerList.Instance.RemovePlayer(PlayerManager.LocalPlayer.name);
			}
		}
	}

}