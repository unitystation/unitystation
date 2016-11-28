using UnityEngine;
using System.Collections;

public class Managers : MonoBehaviour {

	public static Managers control;
	// Use this for initialization

	public GameObject logInWindow;
	public GameObject backGround;
	public GameObject[] UIObjs;


	void Awake(){

		if (control == null) {
		
			Application.runInBackground = true; // this must run in background or it will drop connection if not focussed.

			DontDestroyOnLoad (gameObject);
			control = this;
		
		} else {

			Destroy(gameObject);

		}

	}
	void Start () {
	
	}
		
	// Update is called once per frame
	void Update () {
	
	}

	public void SetScreenForGame(){
		foreach (GameObject obj in UIObjs) {
			obj.SetActive (true);
		}
		backGround.SetActive (false);
		logInWindow.SetActive (false);

	}

	public void SetScreenForLobby(){

		foreach (GameObject obj in UIObjs) {
			obj.SetActive (false);
		}
		backGround.SetActive (true);
		logInWindow.SetActive (true);

	}
}
