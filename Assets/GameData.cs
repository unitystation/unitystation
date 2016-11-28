using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GameData : MonoBehaviour {

	public static GameData control;
//	[HideInInspector]
	public bool isInGame;


	void Awake() {
	
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");

		if (control == null) {
			control = this;
	
		} else {

			Destroy (this);

		}
	
		LoadData ();

	}

	void ApplicationWillResignActive () {

		SaveData ();

	}


	void OnDisable () {

		SaveData ();

	}



	void OnApplicationQuit() {

		SaveData ();

	}

	void Start(){

		OnLevelWasLoaded ();
	}

	void OnLevelWasLoaded(){
		int currentScene = SceneManager.GetActiveScene ().buildIndex;
		if (currentScene == 0) {
			isInGame = false;
			Managers.control.SetScreenForLobby ();
		} else {
		
			isInGame = true;
			Managers.control.SetScreenForGame ();
		}


	}

	void LoadData(){
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		if (File.Exists (Application.persistentDataPath + "/genData01.dat")) {
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Open (Application.persistentDataPath + "/genData01.dat", FileMode.Open);
			UserData data = (UserData)bf.Deserialize (file);
			//DO SOMETHNG WITH THE VALUES HERE, I.E STORE THEM IN A CACHE IN THIS CLASS
			//TODO: LOAD SOME STUFF


			file.Close ();

		}
			
	}


	void SaveData(){

		BinaryFormatter bf = new BinaryFormatter ();
		FileStream file = File.Create (Application.persistentDataPath + "/genData01.dat");
		UserData data = new UserData ();
		/// PUT YOUR MEMBER VALUES HERE, ADD THE PROPERTY TO USERDATA CLASS AND THIS WILL SAVE IT


		//TODO: SAVE SOME STUFF

		bf.Serialize (file, data);
		file.Close ();
	}
}

[Serializable]
class UserData
{
   //TODO: add your members here

}
