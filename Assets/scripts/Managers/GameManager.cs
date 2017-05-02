using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;

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
	private float remainingTime = 900f; //15min rounds
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

	void Update(){
		if (Instance.counting) {
			remainingTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(remainingTime / 60).ToString("00") + ":" + (remainingTime % 60).ToString("00");
		
		}
	}
}
