using System;
using System.Collections;
using DatabaseAPI;
using Lobby;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour
{
	private static GameData gameData;

	public bool testServer;
	private RconManager rconManager;

	enum ValidationStatus
	{
		none,
		success,
		failed
	}

	//Token validation status:
	private ValidationStatus validationStatus;

	/// <summary>
	///     Check to see if you are in the game or in the lobby
	/// </summary>
	public static bool IsInGame { get; private set; }

	public static bool IsHeadlessServer { get; private set; }

	public static string LoggedInUsername { get; set; }

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

		CheckCommandLineArgs();
	}

	private void CheckCommandLineArgs()
	{
		//Check for Hub Message
		string serverIp = GetArgument("-server");
		string port = GetArgument("-port");
		string token = GetArgument("-refreshtoken");
		string uid = GetArgument("-uid");

		Debug.Log($"ServerIP: {serverIp} port: {port} token: {token} uid: {uid}");
		//This is a hub message, attempt to login and connect to server
		if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(uid))
		{
			validationStatus = ValidationStatus.none;
			StartCoroutine(ConnectToServerFromHub(serverIp, port, uid, token));
		}
	}

	void TokenValidationSuccess(string msg)
	{
		validationStatus = ValidationStatus.success;
	}

	void TokenValidationFailed(string msg)
	{
		validationStatus = ValidationStatus.failed;
	}

	//Monitors hub connection steps:
	IEnumerator ConnectToServerFromHub(string ip, string port, string uid, string token)
	{
		Logger.Log("Hub message found. Attempting to log in..", Category.Hub);

		//Hide all default lobby ui's:
		if(!IsInGame) LobbyManager.Instance.lobbyDialogue.HideAllPanels();

		yield return WaitFor.EndOfFrame;

		ServerData.TryTokenValidation(token, uid, TokenValidationSuccess, TokenValidationFailed);

		while (validationStatus == ValidationStatus.none)
		{
			yield return WaitFor.EndOfFrame;
		}

		if (validationStatus == ValidationStatus.failed)
		{
			//TODO: Show login screen
			Logger.Log("Login failed, token or uid is invalid.", Category.Hub);
			yield break;
		}

		Logger.Log("Token validated. User successfully authenticated", Category.Hub);

		ushort p = 0;
		ushort.TryParse(port, out p);

		//Connect to server:
		CustomNetworkManager.Instance.networkAddress = ip;
		CustomNetworkManager.Instance.GetComponent<TelepathyTransport>().port = p;
		CustomNetworkManager.Instance.StartClient();

	}

	private void OnEnable()
	{
		Logger.RefreshPreferences();
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

		//Check if running in batchmode (headless server)
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Instance.testServer)
		{
			float calcFrameRate = 1f / Time.deltaTime;
			Application.targetFrameRate = (int) calcFrameRate;
			Logger.Log($"Starting server in HEADLESS mode. Target framerate is {Application.targetFrameRate}",
				Category.Server);
			IsHeadlessServer = true;
			StartCoroutine(WaitToStartServer());

			if (rconManager == null)
			{
				GameObject rcon = Instantiate(Resources.Load("Rcon/RconManager") as GameObject, null) as GameObject;
				rconManager = rcon.GetComponent<RconManager>();
				Logger.Log("Start rcon server", Category.Rcon);
			}
		}
	}

	private IEnumerator WaitToStartServer()
	{
		yield return WaitFor.Seconds(0.1f);
		CustomNetworkManager.Instance.StartHost();
	}

	private void SetPlayerPreferences()
	{
		//Ambient Volume
		if (PlayerPrefs.HasKey("AmbientVol"))
		{
			SoundManager.Instance.ambientTrack.volume = PlayerPrefs.GetFloat("AmbientVol");
		}
	}

	private string GetArgument(string name)
	{
		string[] args = Environment.GetCommandLineArgs();
		for (int i = 0; i < args.Length; i++)
		{
			if (args[i].Contains(name))
			{
				return args[i + 1];
			}
		}

		return null;
	}
}