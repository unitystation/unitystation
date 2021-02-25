using DiscordWebhook;
using InGameEvents;
using Mirror;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.IO;
using Managers;
using Messages.Client;
using Messages.Server;
using Messages.Server.AdminTools;
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommAnnouncement(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Notice);

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command ANNOUNCEMENT.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommReport(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			GameManager.Instance.CentComm.MakeCommandReport(text, CentComm.UpdateSound.Notice);

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command REPORT.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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
		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
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

		[Command(requiresAuthority = false)]
		public void CmdStartProfile(string adminId, string adminToken, int frameCount)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			ProfileManager.Instance.StartProfile(frameCount);
		}

		[Command(requiresAuthority = false)]
		public void CmdRequestProfiles(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;
			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			ProfileMessage.Send(admin);
		}

		[Command(requiresAuthority = false)]
		public void CmdDeleteProfile(string adminId, string adminToken, string profileName)
		{
			if (IsAdmin(adminId, adminToken) == false) return;
			if (ProfileManager.runningProfile) return;

			string path = Directory.GetCurrentDirectory() + "/Profiles/" + profileName;
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			ProfileMessage.SendToApplicable();
		}

		[Command(requiresAuthority = false)]
		public void CmdAdminGhostDropItem(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			var connectedPlayer = admin.Player();
			var itemStorage = AdminManager.Instance.GetItemSlotStorage(connectedPlayer);
			var slot = itemStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
			Inventory.ServerDrop(slot, admin.AssumedWorldPosServer());
		}


		[Command(requiresAuthority = false)]
		public void CmdAdminGhostSmashItem(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			var connectedPlayer = admin.Player();
			var itemStorage = AdminManager.Instance.GetItemSlotStorage(connectedPlayer);
			var slot = itemStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
			Inventory.ServerDespawn(slot);
		}

		#endregion
	}
}
