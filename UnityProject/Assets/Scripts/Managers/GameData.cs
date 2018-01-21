using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using PlayGroup;

public class GameData : MonoBehaviour
{
	private static GameData gameData;

	public bool testServer;

	/// <summary>
	///     Check to see if you are in the game or in the lobby
	/// </summary>
	public static bool IsInGame { get; private set; }

	public static bool IsHeadlessServer { get; private set; }

	public static GameData Instance
	{
		get
		{
			if (!gameData)
			{
				gameData = FindObjectOfType<GameData>();
				gameData.Init();
			}

			return gameData;
		}
	}

	public bool IsTestMode => SceneManager.GetActiveScene().name.StartsWith("InitTestScene");

	private void Init()
	{
		if (IsTestMode)
		{
			return;
		}

		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");

		string testServerEnv = Environment.GetEnvironmentVariable("TEST_SERVER");
		if (!string.IsNullOrEmpty(testServerEnv))
		{
			testServer = Convert.ToBoolean(testServerEnv);
		}

		LoadData();
	}

	private void ApplicationWillResignActive()
	{
		if (IsTestMode)
		{
			return;
		}

		SaveData();
	}

	private void OnEnable()
	{
		if (IsTestMode)
		{
			return;
		}

		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		if (IsTestMode)
		{
			return;
		}

		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
		SaveData();
	}

	private void OnApplicationQuit()
	{
		if (IsTestMode)
		{
			return;
		}

		SaveData();
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "Lobby")
		{
			IsInGame = false;
			Managers.instance.SetScreenForLobby();
		}
		else
		{
			IsInGame = true;
			Managers.instance.SetScreenForGame();
			SetPlayerPreferences();
		}

		if (CustomNetworkManager.Instance.isNetworkActive)
		{
			//Reset stuff
			if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Instance.testServer)
			{
				IsHeadlessServer = true;
			}
			if (IsInGame && GameManager.Instance != null && CustomNetworkManager.Instance._isServer)
			{
				GameManager.Instance.ResetRoundTime();
			}
			return;
		}
		//force vsync when not-headless
		if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !Instance.testServer && !IsHeadlessServer)
		{
			Application.targetFrameRate = 60;
			QualitySettings.vSyncCount = 1;
		}
		//Check if running in batchmode (headless server)
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Instance.testServer)
		{
			float calcFrameRate =  1f / Time.fixedDeltaTime;
			Application.targetFrameRate = (int) calcFrameRate;
			Debug.Log("START SERVER HEADLESS MODE");
			IsHeadlessServer = true;
			StartCoroutine(WaitToStartServer());
		}
	}

	private IEnumerator WaitToStartServer()
	{
		yield return new WaitForSeconds(0.1f);
		CustomNetworkManager.Instance.StartHost();
	}

	private void LoadData()
	{
		Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		if (File.Exists(Application.persistentDataPath + "/genData01.dat"))
		{
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/genData01.dat", FileMode.Open);
			UserData data = (UserData) bf.Deserialize(file);
			//DO SOMETHNG WITH THE VALUES HERE, I.E STORE THEM IN A CACHE IN THIS CLASS
			//TODO: LOAD SOME STUFF

			file.Close();
		}
	}

	private void SaveData()
	{
		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + "/genData01.dat");
		UserData data = new UserData();
		/// PUT YOUR MEMBER VALUES HERE, ADD THE PROPERTY TO USERDATA CLASS AND THIS WILL SAVE IT

		//TODO: SAVE SOME STUFF
		bf.Serialize(file, data);
		file.Close();
	}

	private void SetPlayerPreferences()
	{
		//Ambient Volume
		if (PlayerPrefs.HasKey("AmbientVol"))
		{
			SoundManager.Instance.ambientTracks[SoundManager.Instance.ambientPlaying].volume =
				PlayerPrefs.GetFloat("AmbientVol");
		}

		if (PlayerPrefs.HasKey("AZERTY"))
		{
			if (PlayerManager.LocalPlayerScript)
			{
				PlayerMove plm = PlayerManager.LocalPlayerScript.playerMove;
				if (PlayerPrefs.GetInt("AZERTY") == 1)
				{
					plm.ChangeKeyboardInput(true);
				}
				else
				{
					plm.ChangeKeyboardInput(false);
				}
			}
		}
	}
}

[Serializable]
internal class UserData
{
	//TODO: add your members here
}