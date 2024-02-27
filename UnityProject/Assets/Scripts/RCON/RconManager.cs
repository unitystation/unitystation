using System.Text.RegularExpressions;
using System.Linq;
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
using Newtonsoft.Json;
using AdminTools;
using Messages.Server.AdminTools;
using System.Collections.Concurrent;

public class RconManager : SingletonManager<RconManager>
{
	private HttpServer httpServer;

	private WebSocketServiceHost rconHost;

	public static ConcurrentQueue<RconMessage> rconQueue = new();

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

		//TODO https/wss support
		//string certPath = Application.streamingAssetsPath + "/config/certificate.pfx";
		//httpServer.SslConfiguration.ServerCertificate =
		//	new X509Certificate2( certPath, config.certKey );

		httpServer.AddWebSocketService<RconSocket>("/rcon");

		httpServer.AuthenticationSchemes = AuthenticationSchemes.Basic;
		httpServer.Realm = "Admins";

		//TODO consider using user credentials and the admin permission system instead of a fixed password, for more access control (blocked on centcomm integration, #7179 )
		httpServer.UserCredentialsFinder = id =>
		{
			var name = id.Name;
			return new NetworkCredential(name, config.RconPass, "admin");
		};

		//httpServer.SslConfiguration.ClientCertificateValidationCallback =
		//	( sender, certificate, chain, sslPolicyErrors ) => { return true; };
		httpServer.Start();

		//Get the service hosts:
		Instance.httpServer.WebSocketServices.TryGetServiceHost("/rcon", out rconHost);


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

	private void SendToSocket(string SocketID, string data, WebSocketServiceHost host = null)
	{
		if (host == null)
		{
			host = rconHost;
		}
		host.Sessions.SendToAsync(data, SocketID, null);
	}

	private void UpdateMe()
	{
		if (rconQueue.Count > 0)
		{
			while (rconQueue.TryDequeue(out RconMessage e))
			{

				if (e.Data == "lastlog")
				{
					SendToSocket(e.SocketID, "lastlog" + RconManager.GetLastLog());
					return;
				}

				if (e.Data == "logfull")
				{
					SendToSocket(e.SocketID, $"logfull{RconManager.GetFullLog()}");
					return;
				}

				if (e.Data.StartsWith("concmd"))
				{
					string command = e.Data[6..];
					ExecuteCommand(command);
					UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
						$"RCON executed server command {command}", "rcon");
					Loggy.Log($"RCON command from {e.Username}: {e.Data}", Category.Rcon);
					return;
				}

				if (e.Data.StartsWith("chatsend"))
				{
					string data = e.Data;
					if (data.Length < 19)
					{
						SendToSocket(e.SocketID, "chatsendfail:not enough data");
						return;
					}
					if (data.Contains("�") == false)
					{
						SendToSocket(e.SocketID, "chatsendfail:invalid request");
						return;
					}

					data = data[8..];
					string channel = data[0..8]; // bitmask in hex
					ChatChannel channels = (ChatChannel)Convert.ToInt32(channel, 16);
					string speaker = data[8..].Split('�')[0].Trim();
					string msg = data[8..].Split('�')[1].Trim();
					var pseudoPlayerInfo = new PlayerInfo
					{
						Connection = null,
						Username = speaker,
						Name = speaker,
						ClientId = e.Username,
						UserId = "rcon",
						ConnectionIP = e.IpAddress,
						PlayerRoles = PlayerRole.Admin
					};

					Chat.AddChatMsgToChatServer(pseudoPlayerInfo, msg, channels);
					Loggy.Log($"RCON chat from {e.Username}: {e.Data}", Category.Rcon);
					SendToSocket(e.SocketID, "chatsendok");
					return;
				}

				if (e.Data.StartsWith("listchannels"))
				{
					List<ChatChannel> channels = Enum.GetValues(typeof(ChatChannel)).Cast<ChatChannel>().ToList();
					var channelList = new List<string>();
					foreach (var channel in channels)
					{
						channelList.Add(channel.ToString() + "=" + ((int)channel).ToString("X8"));
					}
					SendToSocket(e.SocketID, "listchannels" + JsonConvert.SerializeObject(channelList));
					return;
				}

				if (e.Data == "chatfull")
				{
					SendToSocket(e.SocketID, "chatfull" + RconManager.GetFullChatLog());
					return;
				}

				if (e.Data.StartsWith("players"))
				{
					string playerList = JsonUtility.ToJson(new Players());
					if (!string.IsNullOrEmpty(playerList))
					{
						SendToSocket(e.SocketID, "players" + playerList);
					}
					else
					{
						SendToSocket(e.SocketID, "playersfail:No players found");
					}
				}

				if (e.Data.StartsWith("moderateplayer"))
				{
					var data = e.Data[14..];

					RconModerationMessage message;
					try
					{
						message = JsonConvert.DeserializeObject<RconModerationMessage>(data);
					}
					catch (Exception ex)
					{
						SendToSocket(e.SocketID, "moderateplayerfail:Invalid JSON data");
						return;
					}

					if(message.UserID == null || message.Type == null){
						SendToSocket(e.SocketID, "moderateplayerfail:Missing Datafield UserID or Type");
						return;
					}

					if(message.Reason == null){
						message.Reason = "No reason specified.";
					}

					PlayerInfo player = PlayerList.Instance.InGamePlayers.FirstOrDefault(x => x.UserId == message.UserID);
					if (player == null && message.Type != "unban")
					{
						SendToSocket(e.SocketID, "moderateplayerfail:Player does not exist");
						return;
					}

					UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
						$"[RCON] {e.Username} is moderating [{message.UserID}]: Action [{message.Type}] with reason [{message.Reason}]", "rcon");

					switch(message.Type){
						case "ban":
							if(message.Duration == null && !message.Permanent){
								SendToSocket(e.SocketID, "moderateplayerfail:Missing Datafield Duration");
								return;
							}
							var duration = message.Permanent ? 2147483646 : message.Duration;

							PlayerList.Instance.ServerBanPlayer(player, message.Reason, true, duration);
							break;
						case "jobban":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Jobban not implemented yet");
							break;
						case "kick":
							PlayerList.Instance.ServerKickPlayer(player, message.Reason);
							break;
						case "smite":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Smite not implemented yet");
							break;
						case "respawn":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Respawn not implemented yet");
							break;
						case "heal":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Heal not implemented yet");
							break;
						case "ooc_mute":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:OOC Mute not implemented yet");
							break;

						case "unban":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Unban not implemented yet");
							break;
						case "unjobban":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:Unjobban not implemented yet");
							break;
						case "ooc_unmute":
							//TODO
							SendToSocket(e.SocketID, "moderateplayerfail:OOC Unmute not implemented yet");
							break;
						default:
							SendToSocket(e.SocketID, "moderateplayerfail:Invalid Type");
							return;
					}
				}

				if (e.Data.StartsWith("sendhelp"))
				{
					var data = e.Data[14..];

					RconModerationMessage message;
					try
					{
						message = JsonConvert.DeserializeObject<RconModerationMessage>(data);
					}
					catch (Exception ex)
					{
						SendToSocket(e.SocketID, "sendhelpfail:Invalid JSON data");
						return;
					}

					if(message.UserID == null || message.Type == null || message.Reason == null){
						SendToSocket(e.SocketID, "sendhelpfail:Missing Datafield UserID, Type, or Reason");
						return;
					}

					PlayerInfo player = PlayerList.Instance.InGamePlayers.FirstOrDefault(x => x.UserId == message.UserID);
					if (player == null)
					{
						SendToSocket(e.SocketID, "sendhelpfail:Player does not exist");
						return;
					}

					var pseudoPlayerInfo = new PlayerInfo
					{
						Connection = null,
						Username = "[RCON]" + e.Username,
						Name = "[RCON]" + e.Username,
						ClientId = "",
						UserId = "rcon",
						ConnectionIP = e.IpAddress,
						PlayerRoles = PlayerRole.Admin
					};

					string msg = message.Reason;

					switch (message.Type)
					{
						case "admin":
							AdminBwoinkMessage.Send(player.GameObject, pseudoPlayerInfo.UserId, $"<color=red>{pseudoPlayerInfo.Username}: {GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + msg}</color>");
							UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(msg, player, pseudoPlayerInfo);
							break;
						case "mentor":
							MentorBwoinkMessage.Send(player.GameObject, pseudoPlayerInfo.UserId, $"<color=#6400FF>{pseudoPlayerInfo.Username}: {GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + msg}</color>");
							UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg, player, pseudoPlayerInfo);
							break;
						case "prayer":
							PrayerBwoinkMessage.Send(player.GameObject, pseudoPlayerInfo.UserId, $"<i><color=yellow>{msg}</color></i>");
							UIManager.Instance.adminChatWindows.playerPrayerWindow.ServerAddChatRecord(msg, player, pseudoPlayerInfo);
							break;
						case "internal":
							UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord($"[RCON] {e.Username} : {msg}", "rcon");

							break;
						default:
							SendToSocket(e.SocketID, "sendhelpfail:Invalid help type");
							return;
					}
					SendToSocket(e.SocketID, "sendhelpok");
					return;
				}
			}
		}

		if (rconHost != null)
		{
			monitorUpdate += Time.deltaTime;
			if (monitorUpdate > 4f)
			{
				monitorUpdate = 0f;
				BroadcastToSessions("periodicmonitor" + GetMonitorReadOut(), rconHost.Sessions.Sessions);
			}
		}
	}

	public static void AddChatLog(string msg)
	{
		//TODO Isn't working anymore. Works fine if you sendchat, but players in game don't seem to ever trigger this anymore.
		if (Instance.rconHost == null) return;

		msg = $"{DateTime.UtcNow}:    {msg}<br>";
		AmendChatLog(msg);
		BroadcastToSessions("chatupdate" + msg, Instance.rconHost.Sessions.Sessions);
	}

	public static void AddLog(string msg)
	{
		msg = $"{DateTime.UtcNow}:    {msg}<br>";
		AmendLog(msg);
		if (Instance.rconHost != null)
		{
			BroadcastToSessions("logupdate" + msg, Instance.rconHost.Sessions.Sessions);
		}
	}

	public static void UpdatePlayerListRcon()
	{
		if (Instance.rconHost == null) return;
		var json = JsonUtility.ToJson(new Players());
		BroadcastToSessions("playerlistupdate" + json, Instance.rconHost.Sessions.Sessions);
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
		foreach (var s in Instance.rconHost.Sessions.Sessions)
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
		string log = String.Join("\n", ServerLog);
		if (log.Length > 5000)
		{
			log = log.Substring(4000); //TODO? "full" log implies we want everything, consider removing this?
		}
		return log;
	}

	public static string GetFullChatLog()
	{
		string log = String.Join("\n", ChatLog);

		if (string.IsNullOrEmpty(log))
		{
			return "No one has said anything yet..";
		}

		if (log.Length > 10000)
		{
			log = log.Substring(9000); //TODO? "full" log implies we want everything, consider removing this?
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
		command = command[1..];
		DebugLogConsole.ExecuteCommand(command);
	}

	internal void ReceiveRconMessage(RconMessage msg)
	{
		rconQueue.Enqueue(msg);
	}

	#endregion
}

public class RconSocket : WebSocketBehavior
{
	protected override void OnOpen()
	{
		if (Context.User.Identity.IsAuthenticated)
		{
			Loggy.Log($"rcon logged in, {Context.User.Identity.Name} from {Context.UserEndPoint.Address}", Category.Rcon);
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
				$"RCON login by {Context.User.Identity.Name} from {Context.UserEndPoint.Address}", "rcon");
		}

		base.OnOpen();
	}

	protected override void OnMessage(MessageEventArgs e)
	{
		RconMessage msg = new RconMessage
		{
			Username = Context.User.Identity.Name,
			Data = e.Data,
			IpAddress = Context.UserEndPoint.Address.ToString(),
			RequestUri = Context.RequestUri.ToString(),
			SocketID = this.ID
		};
		RconManager.Instance.ReceiveRconMessage(msg);
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
				playerName = player.Name + $" {player.Job} : Acc: {player.Username} {player.UserId} {player.ConnectionIP} ",
				characterName = player.Name,
				username = player.Username,
				userID = player.UserId,
				connectionIP = player.ConnectionIP,
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
	public string characterName;
	public string username;
	public string userID;
	public string connectionIP;
	public string job;
	public ulong steamId;
}

public class RconMessage
{
	public string Username;
	public string Data;
	public string IpAddress;
	public string RequestUri;
	public string SocketID;
}

public class RconModerationMessage
{
	[JsonProperty]
	public string Type;
	[JsonProperty]
	public string UserID;
	[JsonProperty]
	public string Reason;
	[JsonProperty]
	public int Duration;
	[JsonProperty]
	public bool Permanent;
	[JsonProperty]
	public string HandleType; // For job bans, NONE/FORCE_GHOST/KICK
	[JsonProperty]
	public string Job; //Doubles for both non-quick respawn and jobbans
	[JsonProperty]
	public bool QuickRespawn;

}