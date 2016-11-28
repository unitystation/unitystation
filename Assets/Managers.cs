using UnityEngine;
using System.Collections;

public class Managers : MonoBehaviour {

	public static Managers control;
	// Use this for initialization

	[Header("For scene setup functions")]
	public GameObject logInWindow;
	public GameObject backGround;
	public GameObject[] UIObjs;

	//Temp buttons
	public GameObject tempSceneButton;
	public GameObject tempMenuButton;


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
		SoundManager.control.StopMusic ();
		//TODO random ambient
		SoundManager.control.PlayVarAmbient (0);

		//TODO remove the temp button when scene transitions completed
		tempSceneButton.SetActive (false);
		tempMenuButton.SetActive (true);

	}

	public void SetScreenForLobby(){
		SoundManager.control.StopAmbient ();
		SoundManager.control.PlayRandomTrack ();
		foreach (GameObject obj in UIObjs) {
			obj.SetActive (false);
		}
		backGround.SetActive (true);
		logInWindow.SetActive (true);

		//TODO remove the temp button when scene transitions completed
		tempSceneButton.SetActive (false);
		tempMenuButton.SetActive (false);

	}
}
