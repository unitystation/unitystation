using UnityEngine;
using System.Collections;
using UI;

public class Managers : MonoBehaviour {

	public static Managers control;
	// Use this for initialization

	[Header("For scene setup functions")] //FIXME Maybe move this to game manager? or it's own component
	public GameObject logInWindow;
	public GameObject backGround;
	public GameObject[] UIObjs;
	public GameObject UIParent;

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

		UIParent.SetActive (true);
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
		UIParent.SetActive (true);
		SoundManager.control.StopAmbient ();
		SoundManager.control.PlayRandomTrack ();
		UIManager.control.ResetUI (); //Make sure UI is back to default for next play
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
