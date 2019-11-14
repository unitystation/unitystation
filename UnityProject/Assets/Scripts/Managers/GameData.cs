using System;
using System.Collections;
using System.Net.Http;
using System.Threading;
using DatabaseAPI;
using Firebase.Auth;
using Firebase.Extensions;
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
			HubToServerConnect(serverIp, port, uid, token);
		}
	}
	
	private async void HubToServerConnect(string ip, string port, string uid, string token)
	{
		//TODO: Show logging in screen through LobbyManager

		var refreshToken = new RefreshToken();
		refreshToken.refreshToken = token;
		refreshToken.userID = uid;

		HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Get, JsonUtility.ToJson(refreshToken));

		CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

		HttpResponseMessage res;
		try
		{
			res = await ServerData.HttpClient.SendAsync(r, cancellationToken);
		}
		catch(Exception e)
		{
			Logger.LogError($"Something went wrong with hub token validation {e.Message}", Category.Hub);
			LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
			return;
		}

		string msg = await res.Content.ReadAsStringAsync();
		var response = JsonUtility.FromJson<ApiResponse>(msg);

		if (!string.IsNullOrEmpty(response.errorMsg))
		{
			Logger.LogError($"Something went wrong with hub token validation {response.errorMsg}", Category.Hub);
			LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
			return;
		}

		await FirebaseAuth.DefaultInstance.SignInWithCustomTokenAsync(response.message).ContinueWithOnMainThread(
			async task =>
			{
				if (task.IsCanceled)
				{
					Logger.LogError("Custom token sign in was canceled.", Category.Hub);
					LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
					return;
				}

				if (task.IsFaulted)
				{
					Logger.LogError("Task Faulted: " + task.Exception, Category.Hub);
					LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
					return;
				}

				var success = await ServerData.ValidateUser(task.Result, null, null);

				if (success)
				{
					Logger.Log("Signed in successfully with valid token", Category.Hub);
				}
				else
				{
					LobbyManager.Instance.lobbyDialogue.ShowLoginScreen();
				}
			});

		//TODO WAIT UNTIL CHAR SCREEN IS SHOWN:

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