using System;
using System.IO;
using UnityEngine;
using Mirror;
using DiscordWebhook;
using InGameEvents;
using Managers;
using Messages.Server;
using Messages.Server.AdminTools;
using Strings;
using HealthV2;
using AddressableReferences;
using Messages.Server.SoundMessages;
using Audio.Containers;

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

		public static readonly string AdminActionChatColor = "#0077ff";

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
		}

		/// <summary>
		/// Checks whether the adminId and adminToken are valid
		/// </summary>
		/// <param name="sender">The client which sends the command, this is populated by mirror so doesnt need to be manually
		/// put in the parameters when calling the commands</param>
		public static bool IsAdmin(NetworkConnection sender, out ConnectedPlayer player, bool logFailure = true)
		{
			player = PlayerList.Instance.GetByConnection(sender);
			if (PlayerList.Instance.IsAdmin(player) == false)
			{
				if (logFailure)
				{
					var message =
						$"Failed Admin check with id: {player?.ClientId}, associated player with that id (null if not valid id): {player?.Username}," +
						$"Possible hacked client with ip address: {sender?.address}, netIdentity object name: {sender?.identity.OrNull()?.name}]";
					Logger.LogError(message, Category.Exploits);
					LogAdminAction(message);
				}

				return false;
			}

			return true;
		}

		#region GamemodePage

		[Command(requiresAuthority = false)]
		public void CmdToggleOOCMute(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			Chat.Instance.OOCMute = !Chat.Instance.OOCMute;

			var msg = $"OOC has been {(Chat.Instance.OOCMute ? "muted" : "unmuted")}";

			Chat.AddGameWideSystemMsgToChat($"<color={AdminActionChatColor}>{msg}</color>");
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, msg, "");

			LogAdminAction($"{player.Username}: {(Chat.Instance.OOCMute ? "Muted" : "Unmuted")} OOC");
		}

		#endregion

		#region EventsPage

		[Command(requiresAuthority = false)]
		public void CmdTriggerGameEvent(int eventIndex, bool isFake, bool announceEvent,
				InGameEventType eventType, string serializedEventParameters, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			InGameEventsManager.Instance.TriggerSpecificEvent(
					eventIndex, eventType, isFake, player.Username, announceEvent, serializedEventParameters);
		}

		#endregion

		#region RoundPage

		[Command(requiresAuthority = false)]
		public void CmdStartRound(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			if (GameManager.Instance.CurrentRoundState == RoundState.PreRound && GameManager.Instance.waitForStart)
			{
				GameManager.Instance.StartRound();

				Chat.AddGameWideSystemMsgToChat($"<color={AdminActionChatColor}>An admin started the round early.</color>");
				LogAdminAction($"{player.Username}: Force STARTED the round.");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdEndRound(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;
			if (GameManager.Instance.CurrentRoundState == RoundState.Started)
			{
				GameManager.Instance.RoundEndTime = 5; // Quick round end when triggered by admin.

				VideoPlayerMessage.Send(VideoType.RestartRound);
				GameManager.Instance.EndRound();

				Chat.AddGameWideSystemMsgToChat($"<color={AdminActionChatColor}>An admin ended the round early.</color>");
				LogAdminAction($"{player.Username}: Force ENDED the round.");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeNextMap(string nextMap, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			if (SubSceneManager.AdminForcedMainStation == nextMap) return;

			LogAdminAction($"{player.Username}: Changed the next round map from {SubSceneManager.AdminForcedMainStation} to {nextMap}.");

			SubSceneManager.AdminForcedMainStation = nextMap;
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeAwaySite(string nextAwaySite, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			if (SubSceneManager.AdminForcedAwaySite == nextAwaySite) return;

			LogAdminAction($"{player.Username}: Changed the next round away site from {SubSceneManager.AdminForcedAwaySite} to {nextAwaySite}.");

			SubSceneManager.AdminForcedAwaySite = nextAwaySite;
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeAlertLevel(CentComm.AlertLevel alertLevel, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var currentLevel = GameManager.Instance.CentComm.CurrentAlertLevel;

			if (currentLevel == alertLevel) return;

			LogAdminAction($"{player.Username}: Changed the alert level from {currentLevel} to {alertLevel}.");

			GameManager.Instance.CentComm.ChangeAlertLevel(alertLevel);
		}

		#endregion

		#region CentCom

		[Command(requiresAuthority = false)]
		public void CmdCallShuttle(string text, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.Status == EscapeShuttleStatus.DockedCentcom)
			{
				shuttle.CallShuttle(out _);

				var minutes = TimeSpan.FromSeconds(shuttle.InitialTimerSeconds).ToString();
				CentComm.MakeShuttleCallAnnouncement(minutes, text, true);

				LogAdminAction($"{player.Username}: CALLED the emergency shuttle. \n {text}");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdRecallShuttle(string text, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var success = GameManager.Instance.PrimaryEscapeShuttle.RecallShuttle(out var result, true);

			if (success == false) return;

			CentComm.MakeShuttleRecallAnnouncement(text);

			LogAdminAction($"{player.Username}: RECALLED the emergency shuttle. \n {text}");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommAnnouncement(string text, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.CentComAnnounce);

			LogAdminAction($"{player.Username}: made a central command ANNOUNCEMENT. \n {text}");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommReport(string text, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			GameManager.Instance.CentComm.MakeCommandReport(text);

			LogAdminAction($"{player.Username}: made a central command REPORT. \n {text}");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendBlockShuttleCall(bool toggleBool, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockCall == toggleBool) return;

			shuttle.blockCall = toggleBool;

			LogAdminAction($"{player.Username}: {(toggleBool ? "BLOCKED" : "UNBLOCKED")} shuttle calling.");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendBlockShuttleRecall( bool toggleBool, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockRecall == toggleBool) return;

			shuttle.blockRecall = toggleBool;

			LogAdminAction($"{player.Username}: {(toggleBool ? "BLOCKED" : "UNBLOCKED")} shuttle recalling.");
		}

		[Command(requiresAuthority = false)]
		public void CmdCreateDeathSquad(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			Systems.GhostRoles.GhostRoleManager.Instance.ServerCreateRole(deathsquadRole);

			LogAdminAction($"{player.Username}: Created a Death Squad.");
		}

		#endregion

		#region PlayerCommands

		/// <summary>
		/// Smites the selected user, gibbing him instantly.
		/// </summary>
		/// <param name="adminId">Id of the admin performing the action</param>
		/// <param name="adminToken">Token that proves the admin privileges</param>
		/// <param name="userToSmite">User Id of the user to smite</param>
		/// <param name="sender"></param>
		[Command(requiresAuthority = false)]
		public void CmdSmitePlayer(string userToSmite, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			foreach (ConnectedPlayer player in PlayerList.Instance.GetAllByUserID(userToSmite))
			{
				if (player?.Script == null || player.Script.IsGhost || player.Script.playerHealth == null) continue;

				string message = $"{admin.Username}: Smited Username: {player.Username} ({player.Name})";
				Logger.Log(message, Category.Admin);

				LogAdminAction(message);

				player.Script.playerHealth.Gib();
			}
		}

		/// <summary>
		/// Heals a player up
		/// </summary>
		/// <param name="adminId"></param>
		/// <param name="adminToken"></param>
		/// <param name="userToHeal"></param>
		/// <param name="sender"></param>
		[Command(requiresAuthority = false)]
		public void CmdHealUpPlayer(string userToHeal, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			foreach (ConnectedPlayer player in PlayerList.Instance.GetAllByUserID(userToHeal))
			{
				//get player stuff.
				PlayerScript playerScript = player.Script;
				Mind playerMind = playerScript.mind;
				var playerBody = playerMind.body;
				string message;

				//Does this player have a body that can be healed?
				if (playerBody != null && playerBody.TryGetComponent<IFullyHealable>(out var healable))
				{
					healable.FullyHeal();
					message = $"{admin.Username}: Healed up Username: {player.Username} ({player.Name})";
				}
				else
				{
					message = $"{admin.Username}: Attempted healing {player.Username} but they had no body!";
				}
				//Log what we did.
				Logger.Log(message, Category.Admin);
				LogAdminAction(message);
			}
		}

		#endregion

		#region Sound

		[Command(requiresAuthority = false)]
		public void CmdPlaySound(AddressableAudioSource addressableAudioSource, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			SoundManager.PlayNetworked(addressableAudioSource);
		}


		#endregion

		#region Music

		[Command(requiresAuthority = false)]
		public void CmdPlayMusic(AddressableAudioSource addressableAudioSource, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			MusicManager.PlayNetworked(addressableAudioSource);
		}

		#endregion

		#region Profiling

		[Command(requiresAuthority = false)]
		public void CmdStartProfile(int frameCount, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out _) == false) return;

			ProfileManager.Instance.StartProfile(frameCount);
		}

		[Command(requiresAuthority = false)]
		public void CmdRequestProfiles(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player))
			{
				ProfileMessage.Send(player.GameObject);
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdDeleteProfile(string profileName, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out _) == false) return;
			if (ProfileManager.runningProfile || ProfileManager.runningMemoryProfile) return;

			string path = Directory.GetCurrentDirectory() + "/Profiles/" + profileName;
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			ProfileMessage.SendToApplicable();
		}

		[Command(requiresAuthority = false)]
		public void CmdStartMemoryProfile(bool full, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out _) == false) return;

			ProfileManager.Instance.RunMemoryProfile(full);
		}

		#endregion

		#region Inventory

		[Command(requiresAuthority = false)]
		public void CmdAdminGhostDropItem(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var itemStorage = AdminManager.Instance.GetItemSlotStorage(player);
			var slot = itemStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
			Inventory.ServerDrop(slot, player.Script.WorldPos.To2Int());
		}


		[Command(requiresAuthority = false)]
		public void CmdAdminGhostSmashItem(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var itemStorage = AdminManager.Instance.GetItemSlotStorage(player);
			var slot = itemStorage.GetNamedItemSlot(NamedSlot.ghostStorage01);
			Inventory.ServerDespawn(slot);
		}

		#endregion

		#region LogAdminAction

		public static void LogAdminAction(string msg, string userName = "")
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				userName);
		}

		#endregion
	}
}
