using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void GoToKitchen(){

		SoundManager.control.sounds [5].Play ();
		SceneManager.LoadSceneAsync ("Kitchen-Reconstruct");


	}

	public void GoToLobby(){
		SoundManager.control.sounds [5].Play ();
		SceneManager.LoadSceneAsync ("Lobby");

	}
}
