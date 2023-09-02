using System;
using System.Collections.Generic;
using System.Text;
using DatabaseAPI;
using IngameDebugConsole;
using Logs;
using Managers;
using Newtonsoft.Json;
using Shared.Managers;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

public class RconManager : SingletonManager<RconManager>
{
	private HttpServer httpServer;

	private WebSocketServiceHost consoleHost;
	private WebSocketServiceHost monitorHost;
	private WebSocketServiceHost chatHost;
	private WebSocketServiceHost playerListHost;
	private Queue<string> rconChatQueue = new Queue<string>();
	private Queue<string> commandQueue = new Queue<string>();

	private ServerConfig config;

	float monitorUpdate = 0f;

	public override void Start()
	{
		base.Start();
		Instance.Init();
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		if (httpServer != null)
		{
			httpServer.Stop();
		}
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	private void Init()
	{
		Loggy.Log("Init RconManager", Category.Rcon);
		DontDestroyOnLoad(gameObject);

		if (ServerData.ServerConfig == null)
		{
			ServerData.serverDataLoaded += OnServerDataLoaded;
		}
		else
		{
			OnServerDataLoaded();
		}
	}

	private void OnServerDataLoaded()
	{
		ServerData.serverDataLoaded -= OnServerDataLoaded;
		if (ServerData.ServerConfig == null)
		{
			Loggy.Log("No server config found: rcon", Category.Rcon);
			Destroy(gameObject);
		}
		else
		{
			config = ServerData.ServerConfig;
			if (string.IsNullOrEmpty(config.RconPass) || config.RconPort == 0)
			{
				Loggy.Log("Invalid Rcon config, please check your RconPass and RconPort values", Category.Rcon);
				Destroy(gameObject);
			}
			else
			{
				StartServer();
			}
		}
	}

	private void StartServer()
	{
		if (httpServer != null)
		{
			Loggy.Log("Already Listening: WebSocket", Category.Rcon);
			return;
		}

		Loggy.Log("config loaded", Category.Rcon);

		if (GameData.IsHeadlessServer == false && Application.isEditor == false)
		{
			Loggy.Log("Dercon", Category.Rcon);
			Destroy(gameObject);
			return;
		}

		httpServer = new HttpServer(config.RconPort, false);
		//string certPath = Application.streamingAssetsPath + "/config/certificate.pfx";
		//httpServer.SslConfiguration.ServerCertificate =
		//	new X509Certificate2( certPath, config.certKey );
		httpServer.AddWebSocketService<RconSocket>("/rconconsole");
		httpServer.AddWebSocketService<RconMonitor>("/rconmonitor");
		httpServer.AddWebSocketService<RconChat>("/rconchat");
		httpServer.AddWebSocketService<RconPlayerList>("/rconplayerlist");
		httpServer.AuthenticationSchemes = AuthenticationSchemes.Digest;
		httpServer.Realm = "Admins";
		httpServer.UserCredentialsFinder = id =>
		{
			var name = id.Name;
			return name == config.RconPass ?
				new NetworkCredential("admin", null, "admin") :
				null;
		};

		//httpServer.SslConfiguration.ClientCertificateValidationCallback =
		//	( sender, certificate, chain, sslPolicyErrors ) => { return true; };
		httpServer.Start();

		//Get the service hosts:
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconconsole", out consoleHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconmonitor", out monitorHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconchat", out chatHost);
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconplayerlist", out playerListHost);

		if (httpServer.IsListening)
		{
			Loggy.LogFormat("Providing websocket services on port {0}.", Category.Rcon, httpServer.Port);
			foreach (var path in httpServer.WebSocketServices.Paths)
				Loggy.LogFormat("- {0}", Category.Rcon, path);
		}
		else
		{
			Loggy.LogError("Failed to start Rcon server.", Category.Rcon);
			Destroy(gameObject);
		}
	}

	private void UpdateMe()
	{
		if (rconChatQueue.Count > 0)
		{
			var msg = rconChatQueue.Dequeue();
			msg = msg.Substring(1, msg.Length - 1);
			Chat.AddGameWideSystemMsgToChat("[SERVER] " + msg);
		}

		if (commandQueue.Count > 0)
		{
			ExecuteCommand(commandQueue.Dequeue());
		}

		if (monitorHost != null)
		{
			monitorUpdate += Time.deltaTime;
			if (monitorUpdate > 4f)
			{
				monitorUpdate = 0f;
				BroadcastToSessions(GetMonitorReadOut(), monitorHost.Sessions.Sessions);
			}
		}
	}

	public static void AddChatLog(string msg)
	{
		if(Instance.chatHost == null) return;

		msg = $"{DateTime.UtcNow}:    {msg}<br>";
		AmendChatLog(msg);
		Instance.chatHost.Sessions.Broadcast(msg);
		BroadcastToSessions(msg, Instance.chatHost.Sessions.Sessions);
	}

	public static void AddLog(string msg)
	{
		msg = $"{DateTime.UtcNow}:    {msg}<br>";
		AmendLog(msg);
		if (Instance.consoleHost != null)
		{
			BroadcastToSessions(msg, Instance.consoleHost.Sessions.Sessions);
		}
	}

	public static void UpdatePlayerListRcon()
	{
		if(Instance.playerListHost == null) return;
		var json = JsonConvert.SerializeObject(new Players());
		BroadcastToSessions(json, Instance.playerListHost.Sessions.Sessions);
	}

	//On worker thread from websocket:
	public void ReceiveRconChat(string data)
	{
		rconChatQueue.Enqueue(data);
	}

	public void ReceiveRconCommand(string cmd)
	{
		commandQueue.Enqueue(cmd);
	}

	private static void BroadcastToSessions(string msg, IEnumerable<IWebSocketSession> sessions)
	{
		foreach (var conn in sessions)
		{
			if (conn == null)
			{
				continue;
			}
			if (conn.ConnectionState != WebSocketState.Closing ||
				conn.ConnectionState != WebSocketState.Closed)
			{
				conn.Context.WebSocket.Send(msg);
			}
			else
			{
				Loggy.LogFormat("Do not broadcast to (connection not ready): {0}", Category.Rcon, conn.ID);
			}
		}
	}

	//Monitoring:
	public static string GetMonitorReadOut()
	{
		var connectedAdmins = 0;
		foreach (var s in Instance.monitorHost.Sessions.Sessions)
		{
			if (s.ConnectionState == WebSocketState.Open)
			{
				connectedAdmins++;
			}
		}

		//Removed GC Check for time being
		return $"FPS Stats: Current: {FPSMonitor.Instance.Current} Average: {FPSMonitor.Instance.Average}" +
			$" Admins Online: " + connectedAdmins;

		// return $"FPS Stats: Current: {Instance.fpsMonitor.Current} Average: {Instance.fpsMonitor.Average}" +
		// $" GC MEM: {GC.GetTotalMemory( false ) / 1024 / 1024} MB  Admins Online: " + Instance.monitorHost.Sessions.Count;
	}

	public static string GetLastLog()
	{
		return LastLog;
	}

	public static string GetFullLog()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.Append(ServerLog);
		var log = stringBuilder.ToString();
		if (log.Length > 5000)
		{
			log = log.Substring(4000);
		}
		return log;
	}

	public static string GetFullChatLog()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.Append(ChatLog);
		var log = stringBuilder.ToString();

		if (string.IsNullOrEmpty(log))
		{
			return "No one has said anything yet..";
		}

		if (log.Length > 10000)
		{
			log = log.Substring(9000);
		}
		return log;
	}

	#region RconConsole

	protected static List<string> serverLog = new List<string>(1000);
	protected static List<string> ServerLog => serverLog;
	protected static string LastLog { get; private set; }


	protected static List<string> chatLog = new List<string>(1000);
	protected static List<string> ChatLog => chatLog;
	protected static string ChatLastLog { get; private set; }

	protected static void AmendLog(string msg)
	{
		ServerLog.Add(msg);
		LastLog = msg;
	}

	protected static void AmendChatLog(string msg)
	{
		ChatLog.Add(msg);
		ChatLastLog = msg;
	}

	protected static void ExecuteCommand(string command)
	{
		command = command.Substring(1, command.Length - 1);
		DebugLogConsole.ExecuteCommand(command);
	}

	#endregion
}

public class RconSocket : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		if (e.Data == "lastlog")
		{
			Send(RconManager.GetLastLog());
		}

		if (e.Data == "logfull")
		{
			Send(RconManager.GetFullLog());
		}

		if (e.Data[0].Equals('1'))
		{
			RconManager.Instance.ReceiveRconCommand(e.Data);
		}
	}
}

public class RconMonitor : WebSocketBehavior
{
	protected override void OnOpen()
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Loggy.Log("admin logged in", Category.Rcon);
		}

		base.OnOpen();
	}

	protected override void OnClose(CloseEventArgs e)
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Loggy.Log("admin closed. reason: " + e.Reason, Category.Rcon);
		}

		base.OnClose(e);
	}
}

public class RconChat : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		if (e.Data == "chatfull")
		{
			Send(RconManager.GetFullChatLog());
		}

		if (e.Data[0].Equals('1'))
		{
			RconManager.Instance.ReceiveRconChat(e.Data);
		}
	}
}

public class RconPlayerList : WebSocketBehavior
{
	protected override void OnMessage(MessageEventArgs e)
	{
		if (e == null) return;

		if (e.Data == "players")
		{
			var playerList = JsonConvert.SerializeObject(new Players());
			if (!string.IsNullOrEmpty(playerList))
			{
				Send(playerList);
			}
		}
	}
}

[Serializable]
public class Players
{
	public List<PlayerDetails> players = new List<PlayerDetails>();

	public Players()
	{
		if (PlayerList.Instance == null) return;
		for (int i = 0; i < PlayerList.Instance.InGamePlayers.Count; i++)
		{
			var player = PlayerList.Instance.InGamePlayers[i];
			var playerEntry = new PlayerDetails()
			{
				playerName = player.Name + $" {player.Job.ToString()} : Acc: {player.Username} {player.UserId} {player.ConnectionIP} ",
					job = player.Job.ToString()
			};
			players.Add(playerEntry);
		}
	}
}

[Serializable]
public class PlayerDetails
{
	public string playerName;
	public ulong steamID;
	public string job;
}
