using System;
using System.Collections;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SecureStuff;
using DatabaseAPI;
using Lobby;
using Logs;
using Managers;
using Newtonsoft.Json;
using Shared.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour
{
	private static GameData gameData;

	[Tooltip(
		"Only use this when offline or you can't reach the auth server! Allows the game to still work in that situation and " +
		" allows skipping login. Host player will also be given admin privs." +
		"Not supported in release builds.")]
	[SerializeField]
	private bool offlineMode = false;

	/// <summary>
	/// Whether --offlinemode command line argument is passed. Enforces offline mode.
	/// </summary>
	private bool forceOfflineMode;

	/// <summary>
	/// Is offline mode enabled, allowing login skip / working without connection to server.?
	/// Disabled always for release builds.
	/// </summary>
	public bool OfflineMode => (!BuildPreferences.isForRelease && offlineMode) || forceOfflineMode;

	public bool testServer;
	private RconManager rconManager;

	public bool DoNotLoadEditorPreviousScene;

	/// <summary>
	///     Check to see if you are in the game or in the lobby
	/// </summary>
	public static bool IsInGame { get; private set; }

	public static bool IsHeadlessServer
	{
		get { return GameInfo.IsHeadlessServer; }
		private set { GameInfo.IsHeadlessServer = value; }
	}

	public static string LoggedInUsername { get; set; }

	public static int BuildNumber { get; private set; }
	public static string ForkName { get; private set; }

	public static GameData Instance => FindUtils.LazyFindObject(ref gameData);

	public bool DevBuild = false;

	#region Lifecycle

	private void Start()
	{
		_ = Init();
	}

	public async void APITest()
	{
		var url = "https://api.unitystation.org/validatetoken?data=";

		HttpRequestMessage r = new HttpRequestMessage(HttpMethod.Get,
			url + JsonConvert.SerializeObject(""));

		CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;

		HttpResponseMessage res;
		try
		{
			res = await SafeHttpRequest.SendAsync(r, cancellationToken);
		}
		catch (System.Net.Http.HttpRequestException e)
		{
			Loggy.LogError(" APITest Failed setting to off-line mode  " +e.ToString());
			forceOfflineMode = true;
			return;
		}

		forceOfflineMode = false;
	}


	public void SetForceOfflineMode(bool value)
	{
		forceOfflineMode = value;
	}

	private async Task Init()
	{
#if UNITY_EDITOR
		DevBuild = true;
#endif
		var buildInfo = JsonConvert.DeserializeObject<BuildInfo>(AccessFile.Load("buildinfo.json"));
		BuildNumber = buildInfo.BuildNumber;
		ForkName = buildInfo.ForkName;
		forceOfflineMode = !string.IsNullOrEmpty(GetArgument("-offlinemode"));
		Loggy.Log($"Build Version is: {BuildNumber}. " + (OfflineMode ? "Offline mode" : string.Empty));
		CheckHeadlessState();
		APITest();

		AllowedEnvironmentVariables.SetMONO_REFLECTION_SERIALIZER();

		string testServerEnv = AllowedEnvironmentVariables.GetTEST_SERVER();
		if (!string.IsNullOrEmpty(testServerEnv))
		{
			testServer = Convert.ToBoolean(testServerEnv);
		}

		if (await TryJoinViaCmdArgs()) return;
		_ = LobbyManager.Instance.TryAutoLogin();
	}

	private void OnEnable()
	{
		Loggy.RefreshPreferences();

		SceneManager.activeSceneChanged += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnLevelFinishedLoading;
	}

	private IEnumerator WaitToStartServer()
	{
		yield return WaitFor.Seconds(0.1f);
		CustomNetworkManager.Instance.StartHost();
	}

	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
	{
		Resources.UnloadUnusedAssets();
		if (newScene.name == "Lobby")
		{
			IsInGame = false;
			GameScreenManager.Instance.SetScreenForLobby();
		}
		else
		{
			IsInGame = true;
			GameScreenManager.Instance.SetScreenForGame();
		}

		if (CustomNetworkManager.Instance.isNetworkActive)
		{
			//Reset stuff
			CheckHeadlessState();

			if (IsInGame && GameManager.Instance != null && CustomNetworkManager.Instance._isServer)
			{
				GameManager.Instance.ResetRoundTime();
			}

			return;
		}

		//Check if running in batchmode (headless server)
		if (CheckHeadlessState())
		{
			//			float calcFrameRate = 1f / Time.deltaTime;
			//			Application.targetFrameRate = (int) calcFrameRate;
			//			Logger.Log($"Starting server in HEADLESS mode. Target framerate is {Application.targetFrameRate}",
			//				Category.Server);

			Loggy.Log($"FrameRate limiting has been disabled on Headless Server",
				Category.Server);
			IsHeadlessServer = true;
			StartCoroutine(WaitToStartServer());

			if (rconManager == null)
			{
				GameObject rcon = Instantiate(Resources.Load("Rcon/RconManager") as GameObject, null) as GameObject;
				rconManager = rcon.GetComponent<RconManager>();
				Loggy.Log("Start rcon server", Category.Rcon);
			}
		}
	}

	#endregion

	private async Task<bool> TryJoinViaCmdArgs()
	{
		string serverIp = GetArgument("-server");
		string portStr = GetArgument("-port");
		string token = GetArgument("-refreshtoken");
		string uid = GetArgument("-uid");

		if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(portStr)) return false;

		if (ushort.TryParse(portStr, out var port) == false)
		{
			Loggy.LogWarning("Invalid port provided in command line. Cannot join game via args.");
			return false;
		}

		return await HubToServerConnect(serverIp, port, uid, token);
	}

	private async Task<bool> HubToServerConnect(string ip, ushort port, string uid, string token)
	{
		await Task.Delay(TimeSpan.FromSeconds(0.1));

		if (string.IsNullOrEmpty(token) == false)
		{
			Loggy.Log("Logging in via hub account...");
			if (await LobbyManager.Instance.TryTokenLogin(uid, token))
			{
				LobbyManager.Instance.JoinServer(ip, port);
				return true;
			}
			Loggy.LogWarning("Logging in via hub account (via command line args) failed.");
		}

		if (await LobbyManager.Instance.TryAutoLogin())
		{
			LobbyManager.Instance.JoinServer(ip, port);
			return true;
		}

		Loggy.LogWarning("Logging in via stored account token failed.");
		return false;
	}

	private bool CheckHeadlessState()
	{
		if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Instance.testServer)
		{
			IsHeadlessServer = true;
			return true;
		}

		return false;
	}

	#region Helpers

	private static string GetArgument(string name)
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

	#endregion
}
