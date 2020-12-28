using DiscordWebhook;
using InGameEvents;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using Managers;
using Messages.Client;
using Strings;
using UnityEngine;
using UnityEngine.Profiling;

namespace AdminCommands
{
	/// <summary>
	/// Admin Commands manager, stores admin commands, so commands can be run in lobby etc, as its not tied to player object.
	/// </summary>
	public class AdminCommandsManager : NetworkBehaviour
	{
		[SerializeField] private ScriptableObjects.GhostRoleData deathsquadRole = default;

		private static AdminCommandsManager instance;

		public static AdminCommandsManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<AdminCommandsManager>();
				}

				return instance;
			}

			set { instance = value; }
		}

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		public static bool IsAdmin(string adminId, string adminToken)
		{
			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			if (admin == null)
			{
				return false;
			}
			return true;
		}

		#region GamemodePage

		[Server]
		public void CmdToggleOOCMute(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			string msg;

			if (Chat.Instance.OOCMute)
			{
				Chat.Instance.OOCMute = false;
				msg = "OOC has been unmuted";
			}
			else
			{
				Chat.Instance.OOCMute = true;
				msg = "OOC has been muted";
			}

			Chat.AddGameWideSystemMsgToChat($"<color=blue>{msg}</color>");
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, msg, "");
		}

		#endregion

		#region EventsPage

		[Server]
		public void CmdTriggerGameEvent(string adminId, string adminToken, int eventIndex, bool isFake,
			bool announceEvent,
			InGameEventType eventType, string serializedEventParameters)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			InGameEventsManager.Instance.TriggerSpecificEvent(eventIndex, eventType, isFake,
				PlayerList.Instance.GetByUserID(adminId).Username, announceEvent, serializedEventParameters);
		}

		#endregion

		#region RoundPage

		[Server]
		public void CmdStartRound(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound && GameManager.Instance.waitForStart)
			{
				GameManager.Instance.StartRound();

				Chat.AddGameWideSystemMsgToChat("<color=blue>An Admin started the round early.</color>");

				var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Force STARTED the round.";

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL,
					msg,
					"");
			}
		}

		[Server]
		public void CmdEndRound(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			GameManager.Instance.RoundEndTime = 5; // Quick round end when triggered by admin.

			VideoPlayerMessage.Send(VideoType.RestartRound);
			GameManager.Instance.EndRound();

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Force ENDED the round.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdChangeNextMap(string adminId, string adminToken, string nextMap)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (SubSceneManager.AdminForcedMainStation == nextMap) return;

			var msg =
				$"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round map from {SubSceneManager.AdminForcedMainStation} to {nextMap}.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");

			SubSceneManager.AdminForcedMainStation = nextMap;
		}

		[Server]
		public void CmdChangeAwaySite(string adminId, string adminToken, string nextAwaySite)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (SubSceneManager.AdminForcedAwaySite == nextAwaySite) return;

			var msg =
				$"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round away site from {SubSceneManager.AdminForcedAwaySite} to {nextAwaySite}.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");

			SubSceneManager.AdminForcedAwaySite = nextAwaySite;
		}

		[Server]
		public void CmdChangeAlertLevel(string adminId, string adminToken, CentComm.AlertLevel alertLevel)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var currentLevel = GameManager.Instance.CentComm.CurrentAlertLevel;

			if (currentLevel == alertLevel) return;

			var msg =
				$"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the alert level from {currentLevel} to {alertLevel}.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");

			GameManager.Instance.CentComm.ChangeAlertLevel(alertLevel);
		}

		#endregion

		#region CentCom

		[Server]
		public void CmdCallShuttle(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.Status == EscapeShuttleStatus.DockedCentcom)
			{
				shuttle.CallShuttle(out var result);

				var minutes = TimeSpan.FromSeconds(shuttle.InitialTimerSeconds).ToString();
				CentComm.MakeShuttleCallAnnouncement(minutes, text, true);

				var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: CALLED the emergency shuttle.";

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL,
					msg, "");
			}
		}

		[Server]
		public void CmdRecallShuttle(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var success = GameManager.Instance.PrimaryEscapeShuttle.RecallShuttle(out var result, true);

			if (!success) return;

			CentComm.MakeShuttleRecallAnnouncement(text);

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: RECALLED the emergency shuttle.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdSendCentCommAnnouncement(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Notice);

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command ANNOUNCEMENT.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdSendCentCommReport(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			GameManager.Instance.CentComm.MakeCommandReport(text, CentComm.UpdateSound.Notice);

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command REPORT.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdSendBlockShuttleCall(string adminId, string adminToken, bool toggleBool)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockCall == toggleBool) return;

			shuttle.blockCall = toggleBool;

			var state = toggleBool ? "BLOCKED" : "UNBLOCKED";
			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: {state} shuttle calling.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdSendBlockShuttleRecall(string adminId, string adminToken, bool toggleBool)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockRecall == toggleBool) return;

			shuttle.blockRecall = toggleBool;

			var state = toggleBool ? "BLOCKED" : "UNBLOCKED";
			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: {state} shuttle recalling.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Server]
		public void CmdCreateDeathSquad(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			Systems.GhostRoles.GhostRoleManager.Instance.ServerCreateRole(deathsquadRole);
		}

		#endregion

		#region PlayerCommands

		/// <summary>
		/// Smites the selected user, gibbing him instantly.
		/// </summary>
		/// <param name="adminId">Id of the admin performing the action</param>
		/// <param name="adminToken">Token that proves the admin privileges</param>
		/// <param name="userToSmite">User Id of the user to smite</param>
		[Server]
		public void CmdSmitePlayer(string adminId, string adminToken, string userToSmite)
		{
			GameObject admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			if (admin == null) return;

			var players = PlayerList.Instance.GetAllByUserID(userToSmite);
			if (players.Count != 0)
			{
				foreach (ConnectedPlayer player in players)
				{
					string message = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Smited Username: {player.Username} ({player.Name})";
					Logger.Log(message);
					UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(message, null); DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message, "");
					player.Script.playerHealth.ServerGibPlayer();
				}
			}
		}
		#endregion

		#region Sound

		[Server]
		public void CmdPlaySound(string adminId, string adminToken, string index)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var players = FindObjectsOfType(typeof(PlayerScript));

			if (players == null) return; //If list of Players is empty dont run rest of code.

			foreach (PlayerScript player in players)
			{
				// SoundManager.PlayNetworkedForPlayerAtPos(player.gameObject,
					// player.gameObject.GetComponent<RegisterTile>().WorldPositionClient, index);
			}

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: played the global sound: {index}.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		#endregion

		#region Profiling

		public bool runningProfile = false;

		[Server]
		public void CmdStartProfile(string adminId, string adminToken, int frameCount)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (runningProfile) return;
			if (frameCount > 300)
				frameCount = 300;

			runningProfile = true;

			Directory.CreateDirectory("Profiles");
			Profiler.SetAreaEnabled(ProfilerArea.Memory, true);
			Profiler.logFile = "Profiles/" + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss");
			Profiler.enableBinaryLog = true;
			Profiler.enabled = true;

			UpdateManager.Instance.Profile = true;

			StartCoroutine(RunPorfile(frameCount));
		}

		private IEnumerator RunPorfile(int frameCount)
		{
			while (frameCount > 0)
			{
				frameCount--;
				yield return null;
			}

			runningProfile = false;
			Profiler.enabled = false;
			Profiler.enableBinaryLog = true;
			Profiler.logFile = "";

			UpdateManager.Instance.Profile = false;

			ProfileMessage.SendToApplicable();
		}

		[Server]
		public void CmdRequestProfiles(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;
			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			ProfileMessage.Send(admin);
		}

		[Server]
		public void CmdDeleteProfile(string adminId, string adminToken, string profileName)
		{
			if (IsAdmin(adminId, adminToken) == false) return;
			if (runningProfile) return;

			string path = Directory.GetCurrentDirectory() + "/Profiles/" + profileName;
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			ProfileMessage.SendToApplicable();
		}

		#endregion
	}

	/// <summary>
	/// Generic net message for verification parameters only.
	/// </summary>
	public class ServerCommandVersionOneMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public string Action;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionOneMessageClient Send(string adminId, string adminToken,
			string action)
		{
			ServerCommandVersionOneMessageClient msg = new ServerCommandVersionOneMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				Action = action
			};
			msg.Send();
			return msg;
		}
	}

	/// <summary>
	/// Generic net message for verification parameters, and a generic string parameter.
	/// </summary>
	public class ServerCommandVersionTwoMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public string Parameter;
		public string Action;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken,
				Parameter
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionTwoMessageClient Send(string adminId, string adminToken, string parameter,
			string action)
		{
			ServerCommandVersionTwoMessageClient msg = new ServerCommandVersionTwoMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				Parameter = parameter,
				Action = action
			};
			msg.Send();
			return msg;
		}
	}

	/// <summary>
	/// Custom net message with verification parameters, and a enum for AlertLevel.
	/// </summary>
	public class ServerCommandVersionThreeMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public CentComm.AlertLevel AlertLevel;
		public string Action;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken,
				AlertLevel
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionThreeMessageClient Send(string adminId, string adminToken,
			CentComm.AlertLevel alertLevel, string action)
		{
			ServerCommandVersionThreeMessageClient msg = new ServerCommandVersionThreeMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				AlertLevel = alertLevel,
				Action = action
			};
			msg.Send();
			return msg;
		}
	}

	/// <summary>
	/// Custom net message with verification parameters, and a parameters for event trigger.
	/// </summary>
	public class ServerCommandVersionFourMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public int EventIndex;
		public bool IsFake;
		public bool AnnounceEvent;
		public InGameEventType EventType;
		public string Action;

		/// <summary>
		/// JSon Serialization of the extra event parameters
		/// </summary>
		public string SerializedEventParameters;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken,
				EventIndex,
				IsFake,
				AnnounceEvent,
				EventType,
				SerializedEventParameters
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionFourMessageClient Send(string adminId, string adminToken, int eventIndex,
			bool isFake, bool announceEvent, InGameEventType eventType,
			string action, BaseEventParameters eventParameters = null)
		{
			ServerCommandVersionFourMessageClient msg = new ServerCommandVersionFourMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				EventIndex = eventIndex,
				IsFake = isFake,
				AnnounceEvent = announceEvent,
				EventType = eventType,
				Action = action,
				SerializedEventParameters = JsonConvert.SerializeObject(eventParameters)
			};
			msg.Send();
			return msg;
		}
	}

	/// <summary>
	/// Generic net message with verification parameters, and a generic bool parameter.
	/// </summary>
	public class ServerCommandVersionFiveMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public bool GenericBool;
		public string Action;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken,
				GenericBool
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionFiveMessageClient Send(string adminId, string adminToken, bool genericBool, string action)
		{
			ServerCommandVersionFiveMessageClient msg = new ServerCommandVersionFiveMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				GenericBool = genericBool,
				Action = action
			};
			msg.Send();
			return msg;
		}
	}

	/// <summary>
	/// Generic net message with verification parameters, and a generic int parameter.
	/// </summary>
	public class ServerCommandVersionSixMessageClient : ClientMessage
	{
		public string AdminId;
		public string AdminToken;
		public int GenericInt;
		public string Action;

		public override void Process()
		{
			if (AdminCommandsManager.IsAdmin(AdminId, AdminToken) == false)
				return;

			object[] paraObject =
			{
				AdminId,
				AdminToken,
				GenericInt
			};

			var instance = AdminCommandsManager.Instance;

			//server stuff
			if (instance == null) return;

			instance.GetType().GetMethod(Action)?.Invoke(instance, paraObject);
		}

		public static ServerCommandVersionSixMessageClient Send(string adminId, string adminToken, int genericInt, string action)
		{
			ServerCommandVersionSixMessageClient msg = new ServerCommandVersionSixMessageClient
			{
				AdminId = adminId,
				AdminToken = adminToken,
				GenericInt = genericInt,
				Action = action
			};
			msg.Send();
			return msg;
		}
	}
}
