using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

public class RconManager : RconConsole
{
	private static RconManager rconManager;

	public static RconManager Instance
	{
		get
		{
			if (rconManager == null)
			{
				rconManager = FindObjectOfType<RconManager>();
			}

			return rconManager;
		}
	}

	private HttpServer httpServer;
	private FPSMonitor fpsMonitor;

	private WebSocketServiceHost consoleHost;
	private WebSocketServiceHost monitorHost;
	private WebSocketServiceHost chatHost;
	private WebSocketServiceHost playerListHost;
	private Queue<string> rconChatQueue = new Queue<string>();
	private Queue<string> commandQueue = new Queue<string>();

	private ServerConfig config;

	float monitorUpdate = 0f;

	private void OnEnable()
	{
		Instance.Init();
	}

	private void OnDisable()
	{
		if (httpServer != null)
		{
			httpServer.Stop();
		}
	}

	private void Init()
	{
		Logger.Log("Init RconManager", Category.Rcon);
		DontDestroyOnLoad(rconManager.gameObject);
		fpsMonitor = GetComponent<FPSMonitor>();
		string filePath = Application.streamingAssetsPath + "/config/config.json";

		try
		{
			string result = System.IO.File.ReadAllText(filePath);
			if (string.IsNullOrEmpty(result))
			{
				Logger.Log("No server config found: rcon", Category.Rcon);
				Destroy(gameObject);
			}

			config = JsonUtility.FromJson<ServerConfig>(result);
			StartServer();
		}
		catch
		{
			Logger.Log("config failed to load", Category.Rcon);
			Destroy(gameObject);
		}
	}

	private void StartServer()
	{
		if (httpServer != null)
		{
			Logger.Log("Already Listening: WebSocket", Category.Rcon);
			return;
		}

		Logger.Log("config loaded", Category.Rcon);

		if (!GameData.IsHeadlessServer)
		{
			Logger.Log("Dercon", Category.Rcon);
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
			Logger.Log("Providing websocket services on port " + httpServer.Port, Category.Rcon);
			foreach (var path in httpServer.WebSocketServices.Paths)
				Logger.Log("- " + path);
		}
	}

	private void Update()
	{
		if (rconChatQueue.Count > 0)
		{
			var msg = rconChatQueue.Dequeue();
			msg = msg.Substring(1, msg.Length - 1);
			ChatEvent chatEvent = new ChatEvent("[Server]: " + msg, ChatChannel.System);
			ChatRelay.Instance.AddToChatLogServer(chatEvent);
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
		msg = DateTime.UtcNow + ":    " + msg + "<br>";
		AmendChatLog(msg);
		Instance.chatHost.Sessions.Broadcast(msg);
		BroadcastToSessions(msg, Instance.chatHost.Sessions.Sessions);
	}

	public static void AddLog(string msg)
	{
		msg = DateTime.UtcNow + ":    " + msg + "<br>";
		AmendLog(msg);
		if (Instance.consoleHost != null)
		{
			BroadcastToSessions(msg, Instance.consoleHost.Sessions.Sessions);
		}
	}

	public static void UpdatePlayerListRcon()
	{
		var json = JsonUtility.ToJson(new Players());
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
		foreach(var conn in sessions){
			if(conn == null){
				continue;
			}
			if(conn.ConnectionState != WebSocketState.Closing || 
			conn.ConnectionState != WebSocketState.Closed){
				conn.Context.WebSocket.Send(msg);
			} else {
				Debug.Log("Do not broadcast to (connection not ready): " + conn.ID);
			}
		}
	}

	//Monitoring:
	public static string GetMonitorReadOut()
	{
		//Removed GC Check for time being
		return $"FPS Stats: Current: {Instance.fpsMonitor.Current} Average: {Instance.fpsMonitor.Average}" +
			$" Admins Online: " + Instance.monitorHost.Sessions.Count;

			// return $"FPS Stats: Current: {Instance.fpsMonitor.Current} Average: {Instance.fpsMonitor.Average}" +
			// $" GC MEM: {GC.GetTotalMemory( false ) / 1024 / 1024} MB  Admins Online: " + Instance.monitorHost.Sessions.Count;
	}

	public static string GetLastLog()
	{
		return LastLog;
	}

	public static string GetFullLog()
	{
		var log = ServerLog;
		if (log.Length > 5000)
		{
			log = log.Substring(4000);
		}
		return log;
	}

	public static string GetFullChatLog()
	{
		var log = ChatLog;
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
			Logger.Log("admin logged in", Category.Rcon);
		}

		base.OnOpen();
	}

	protected override void OnClose(CloseEventArgs e)
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Logger.Log("admin closed. reason: " + e.Reason, Category.Rcon);
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
		if (e.Data == "players")
		{
			var playerList = JsonUtility.ToJson(new Players());
			Send(playerList);
		}
	}
}

public class ServerConfig
{
	public string RconPass;
	public int RconPort;
	public string certKey;
}

[Serializable]
public class Players
{
	public List<PlayerDetails> players = new List<PlayerDetails>();

	public Players()
	{
		for (int i = 0; i < PlayerList.Instance.InGamePlayers.Count; i++)
		{
			var player = PlayerList.Instance.InGamePlayers[i];
			var playerEntry = new PlayerDetails()
			{
				playerName = player.Name,
					steamID = player.SteamId,
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