using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;
using UnityEngine.SceneManagement;
using PlayGroup;
public class GameManager : NetworkBehaviour {

	private static GameManager gameManager;

	public static GameManager Instance{
		get{ 
			if (!gameManager) {
				gameManager = FindObjectOfType<GameManager>();
			}
			return gameManager;
		}
	}

	public Text roundTimer;
	private bool counting = false;
	private bool waitForRestart = false;
	private float remainingTime = 300f; //5min
	private float cacheTime = 300f;
	private float restartTime = 15f;
	public static float GetRoundTime {get{
			return Instance.remainingTime;
		}}

	public override void OnStartClient(){
		Instance.counting = true;
		base.OnStartClient();
	}

	public void SyncTime(float currentTime){
		Instance.remainingTime = currentTime;
	}

	public void ResetRoundTime(){
		Instance.remainingTime = Instance.cacheTime;
		restartTime = 15f;
		Instance.counting = true;
	}

	void Update(){

		if (Instance.waitForRestart) {
			restartTime -= Time.deltaTime;
			if (Instance.restartTime <= 0f) {
				Instance.waitForRestart = false;
				RestartRound();
			}
		}

		if (Instance.counting) {
			Instance.remainingTime -= Time.deltaTime;
			Instance.roundTimer.text = Mathf.Floor(Instance.remainingTime / 60).ToString("00") + ":" + (Instance.remainingTime % 60).ToString("00");
			if (Instance.remainingTime <= 0f) {
				Instance.counting = false;
				roundTimer.text = "GameOver";
				SoundManager.Play("ApcDestroyed");

				if (CustomNetworkManager.Instance._isServer) {
					PlayerList.Instance.ReportScores();
					Instance.waitForRestart = true;
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
