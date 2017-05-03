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
	private float remainingTime = 180f; //3min rounds
	private float cacheTime = 180f;
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
				SoundManager.Play("ApcDestroyed");

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
