using System;
using System.Collections;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SecureStuff;
using DatabaseAPI;
using Initialisation;
using Lobby;
using Logs;
using Managers;
using Newtonsoft.Json;
using Shared.Util;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class GameData : MonoBehaviour, IInitialise
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
	public bool OfflineMode => (BuildPreferences.isForRelease == false && offlineMode) || forceOfflineMode;

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

	public HubJoinArgs JoinArgs = null;

	public class HubJoinArgs
	{
		public string ServerIP;
		public string Port;
		public string Token;
		public string UID;

		public HubJoinArgs(string IP, string p, string t, string uuid)
		{
			ServerIP = IP;
			Port = p;
			Token = t;
			UID = uuid;
		}
	}

	#region Lifecycle

	public async UniTask<bool> CheckBackendAlive()
	{
		var url = $"https://{GameManager.Instance.AccountAPIHost}/";

		var request = new HttpRequestMessage(HttpMethod.Get, url);
		CancellationToken cancellationToken = new CancellationTokenSource(120000).Token;
		try
		{
			_ = await SafeHttpRequest.SendAsync(request, cancellationToken);
		}
		catch (HttpRequestException e)
		{
			Loggy.Error("Could not connect to the backend. It is either dead or there is no internet. Error: " + e);
			return false;
		}

		return true;
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
		Loggy.Info($"Build Version is: {BuildNumber}. " + (OfflineMode ? "Offline mode" : string.Empty));
		CheckHeadlessState();
		bool isBackendAlive = await CheckBackendAlive();
		if (isBackendAlive == false)
		{
			forceOfflineMode = true;
		}

		AllowedEnvironmentVariables.SetMONO_REFLECTION_SERIALIZER();

		string testServerEnv = AllowedEnvironmentVariables.GetTEST_SERVER();
		if (!string.IsNullOrEmpty(testServerEnv))
		{		_ = LobbyManager.Instance.TryAutoLogin();

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
		CustomNetworkManager.Instance.StartHostWrapper();
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

			if (IsInGame && GameManager.Instance != null && CustomNetworkManager.IsServer)
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
			//			Loggy.Log($"Starting server in HEADLESS mode. Target framerate is {Application.targetFrameRate}",
			//				Category.Server);

			Loggy.Info($"FrameRate limiting has been disabled on Headless Server",
				Category.Server);
			IsHeadlessServer = true;
			StartCoroutine(WaitToStartServer());

			if (rconManager == null)
			{
				GameObject rcon = Instantiate(Resources.Load("Rcon/RconManager") as GameObject, null) as GameObject;
				rconManager = rcon.GetComponent<RconManager>();
				Loggy.Info("Start rcon server", Category.Rcon);
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

		JoinArgs = new HubJoinArgs(serverIp, portStr, token, uid);

		if (string.IsNullOrEmpty(serverIp) || string.IsNullOrEmpty(portStr)) return false;

		if (ushort.TryParse(portStr, out var port) == false)
		{
			Loggy.Warning("Invalid port provided in command line. Cannot join game via args.");
			return false;
		}

		return await HubToServerConnect(serverIp, port, uid, token);
	}

	public async Task TryLoginThenServerConnectFromJoinArgs()
	{
		await Task.Delay(TimeSpan.FromSeconds(0.1));
		LobbyManager.Instance.JoinServer(JoinArgs.ServerIP, ushort.Parse(JoinArgs.Port));
		LoadManager.DoInMainThread(() => Loggy.Info($"HubToServerConnect Connecting to IP {JoinArgs.ServerIP}, port {JoinArgs.Port}"));
	}

	private async Task<bool> HubToServerConnect(string ip, ushort port, string uid, string token)
	{
		Loggy.Info($"HubToServerConnect Connecting to IP {ip}, port {port}, with uid {uid}");
		await Task.Delay(TimeSpan.FromSeconds(0.1));

		if (string.IsNullOrEmpty(token) == false)
		{
			Loggy.Info("Logging in via hub account...");
			if (await LobbyManager.Instance.TryTokenLogin(token)) // TODO uid not needed anymore?
			{
				LobbyManager.Instance.JoinServer(ip, port);
				return true;
			}
			Loggy.Warning("Logging in via hub account (via command line args) failed.");
		}

		if (await LobbyManager.Instance.TryAutoLogin())
		{
			LobbyManager.Instance.JoinServer(ip, port);
			return true;
		}

		Loggy.Warning("Logging in via stored account token failed.");
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

	public InitialisationSystems Subsystem => InitialisationSystems.GameData;
	public void Initialise()
	{
		_ = Init();
	}
}
