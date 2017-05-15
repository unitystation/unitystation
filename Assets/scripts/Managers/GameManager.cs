using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;
using UnityEngine.SceneManagement;
using PlayGroup;
public class GameManager : MonoBehaviour {

	public static GameManager Instance;


	public Text roundTimer;
	private bool counting = false;
	private bool waitForRestart = false;
	private float remainingTime = 360f; //6min rounds
	private float cacheTime = 360f;
	private float restartTime = 10f;
	public float GetRoundTime {get{
			return remainingTime;
		}}

	void Awake(){
		if (Instance == null) {
			Instance = this;
		} else {
			Destroy(this);
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

	void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode){
		if (scene.name != "Lobby") {
			counting = true;
		}

	}
		
	public void SyncTime(float currentTime){
		remainingTime = currentTime;
	}

	public void ResetRoundTime(){
		remainingTime = cacheTime;
		restartTime = 10f;
		counting = true;
	}

	void Update(){
		if (!GameData.IsHeadlessServer) {
			if (Screen.width > 1280 || Screen.height > 720) {
				Screen.SetResolution(1280, 720, false);
			}
		}

		if (waitForRestart) {
			restartTime -= Time.deltaTime;
			if (restartTime <= 0f) {
				waitForRestart = false;
				RestartRound();
			}
		}

		if (counting) {
			remainingTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(remainingTime / 60).ToString("00") + ":" + (remainingTime % 60).ToString("00");
			if (remainingTime <= 0f) {
				counting = false;
				roundTimer.text = "GameOver";
				SoundManager.Play("ApcDestroyed",0.3f,1f,0f);

				if (CustomNetworkManager.Instance._isServer) {
					PlayerList.Instance.ReportScores();
					waitForRestart = true;
				}
			}
		}

		//if there are multiple people and everyone is dead besides one player, restart the match
		if (counting && PlayerList.playerList != null && PlayerList.Instance.connectedPlayers.Count > 1) {
			int playerCount = PlayerList.playerList.connectedPlayers.Count;
			int deadCount = 0;
			foreach (var player in PlayerList.playerList.connectedPlayers) {
				if (player.Value != null) {
					var human = player.Value.GetComponent<Human> ();
					if (human != null) {
						//if a player is dead or effectivly dead count them towards a dead total
						if (human.mobStat == MobConsciousStat.DEAD) {
							deadCount++;
						}
					}
				}
			}

			//if there is only one or less people left, restart
			if (playerCount - deadCount <= 1) {
				counting = false;
				roundTimer.text = "GameOver";
				SoundManager.Play("ApcDestroyed",0.3f,1f,0f);

				if (CustomNetworkManager.Instance._isServer) {
					PlayerList.Instance.ReportScores();
					waitForRestart = true;
				}
			}
		}
	}

	void RestartRound(){
		if (CustomNetworkManager.Instance._isServer) {
			CustomNetworkManager.Instance.ServerChangeScene(SceneManager.GetActiveScene().name);
		}
	}
}
