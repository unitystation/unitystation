﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class GameData: MonoBehaviour
{
	/// <summary>
	/// Check to see if you are in the game or in the lobby
	/// </summary>
	public static bool IsInGame { get; private set; }
	public static bool IsHeadlessServer { get; private set; }
	public bool testServer = false;
	private static GameData gameData;

	public static GameData Instance {
		get {
			if (!gameData) {
				gameData = FindObjectOfType<GameData>();
				gameData.Init();
			}

			return gameData;
		}
	}

	void Init()
	{
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		LoadData();
	}

	void ApplicationWillResignActive()
	{
		SaveData();
	}

	void OnDisable()
	{
		SaveData();
	}

	void OnApplicationQuit()
	{
		SaveData();
	}

	void Start()
	{
		OnLevelWasLoaded();
	}

	void OnLevelWasLoaded()
	{
		//Check if running in batchmode (headless server)
		if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null || Instance.testServer) {
			Debug.Log("START SERVER HEADLESS MODE");
			IsHeadlessServer = true;
			CustomNetworkManager.Instance.StartHost();
			return;
		}
		if (Application.loadedLevelName == "Lobby") {
			IsInGame = false;
			Managers.instance.SetScreenForLobby();
		} else {
			IsInGame = true;
			Managers.instance.SetScreenForGame();
		}
	}

	void LoadData()
	{
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		if (File.Exists(Application.persistentDataPath + "/genData01.dat")) {
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/genData01.dat", FileMode.Open);
			UserData data = (UserData)bf.Deserialize(file);
			//DO SOMETHNG WITH THE VALUES HERE, I.E STORE THEM IN A CACHE IN THIS CLASS
			//TODO: LOAD SOME STUFF

			file.Close();
		}
	}

	void SaveData()
	{
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + "/genData01.dat");
		UserData data = new UserData();
		/// PUT YOUR MEMBER VALUES HERE, ADD THE PROPERTY TO USERDATA CLASS AND THIS WILL SAVE IT
        
		//TODO: SAVE SOME STUFF
		bf.Serialize(file, data);
		file.Close();
	}
}

[Serializable]
class UserData
{
	//TODO: add your members here

}
