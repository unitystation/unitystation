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
				var player = PlayerList.Instance.GetByUserID(adminId);
				Logger.LogError($"Failed Admin check with id: {adminId}, associated player with that id (null if not valid id): {player?.Username}," +
				                $"Possible hacked client", Category.Exploits);
				return false;
			}

			return true;
		}

		#region GamemodePage

		[Command(requiresAuthority = false)]
		public void CmdToggleOOCMute(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			Chat.Instance.OOCMute = !Chat.Instance.OOCMute;

			var msg = $"OOC has been {(Chat.Instance.OOCMute ? "muted" : "unmuted")}";

			Chat.AddGameWideSystemMsgToChat($"<color=blue>{msg}</color>");
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, msg, "");

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: {(Chat.Instance.OOCMute ? "Muted" : "Unmuted")} OOC");
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

				LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Force STARTED the round.");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdEndRound(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;
			if (GameManager.Instance.CurrentRoundState == RoundState.Started)
			{
				GameManager.Instance.RoundEndTime = 5; // Quick round end when triggered by admin.

				VideoPlayerMessage.Send(VideoType.RestartRound);
				GameManager.Instance.EndRound();

				LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Force ENDED the round.");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeNextMap(string adminId, string adminToken, string nextMap)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (SubSceneManager.AdminForcedMainStation == nextMap) return;

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round map from {SubSceneManager.AdminForcedMainStation} to {nextMap}.");

			SubSceneManager.AdminForcedMainStation = nextMap;
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeAwaySite(string adminId, string adminToken, string nextAwaySite)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			if (SubSceneManager.AdminForcedAwaySite == nextAwaySite) return;

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round away site from {SubSceneManager.AdminForcedAwaySite} to {nextAwaySite}.");

			SubSceneManager.AdminForcedAwaySite = nextAwaySite;
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeAlertLevel(string adminId, string adminToken, CentComm.AlertLevel alertLevel)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var currentLevel = GameManager.Instance.CentComm.CurrentAlertLevel;

			if (currentLevel == alertLevel) return;

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the alert level from {currentLevel} to {alertLevel}.");

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

				LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: CALLED the emergency shuttle.");
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdRecallShuttle(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var success = GameManager.Instance.PrimaryEscapeShuttle.RecallShuttle(out var result, true);

			if (!success) return;

			CentComm.MakeShuttleRecallAnnouncement(text);

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: RECALLED the emergency shuttle.");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommAnnouncement(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, text, CentComm.UpdateSound.Notice);

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command ANNOUNCEMENT.");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendCentCommReport(string adminId, string adminToken, string text)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			GameManager.Instance.CentComm.MakeCommandReport(text, CentComm.UpdateSound.Notice);

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: made a central command REPORT.");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendBlockShuttleCall(string adminId, string adminToken, bool toggleBool)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockCall == toggleBool) return;

			shuttle.blockCall = toggleBool;

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: {(toggleBool ? "BLOCKED" : "UNBLOCKED")} shuttle calling.");
		}

		[Command(requiresAuthority = false)]
		public void CmdSendBlockShuttleRecall(string adminId, string adminToken, bool toggleBool)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var shuttle = GameManager.Instance.PrimaryEscapeShuttle;

			if (shuttle.blockRecall == toggleBool) return;

			shuttle.blockRecall = toggleBool;

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: {(toggleBool ? "BLOCKED" : "UNBLOCKED")} shuttle recalling.");
		}

		[Command(requiresAuthority = false)]
		public void CmdCreateDeathSquad(string adminId, string adminToken)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			Systems.GhostRoles.GhostRoleManager.Instance.ServerCreateRole(deathsquadRole);

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: Created a Death Squad.");
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
					string message =
						$"{PlayerList.Instance.GetByUserID(adminId).Username}: Smited Username: {player.Username} ({player.Name})";
					Logger.Log(message, Category.Admin);

					LogAdminAction(message);

					player.Script.playerHealth.ServerGibPlayer();
				}
			}
		}


		/// <summary>
		/// Heals a player up
		/// </summary>
		/// <param name="adminId"></param>
		/// <param name="adminToken"></param>
		/// <param name="userToSmite"></param>
		[Command(requiresAuthority = false)]
		public void CmdHealUpPlayer(string adminId, string adminToken, string userToHeal)
		{
			GameObject admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
			if (admin == null) return;

			var players = PlayerList.Instance.GetAllByUserID(userToHeal);
			if (players.Count != 0)
			{
				foreach (ConnectedPlayer player in players)
				{
					//get player stuff.
					PlayerScript playerScript = player.Script;
					Mind playerMind = playerScript.mind;
					var playerBody = playerMind.body;
					HealthV2.PlayerHealthV2 health = playerBody.playerHealth;
					string message = "";

					//Does this player have a body that can be healed?
					if(playerBody != null)
					{
						if(health.IsDead == false) //If player is not dead; simply heal all his damage and that's all.
						{
							health.ResetDamageAll();
							playerScript.registerTile.ServerStandUp();
						}
						else //If not, start reviving the player.
						{
							//(Max): Because Mirror authority does not allow us to call functions from PlayerSpawn in [Command(requiresAuthority = false)]
							//(Max): We have to avoid forcing the player ghost back into his body for now until we find a work around.
							//If the player is a ghost --force them into their body-- tell admins to tell the player to return back to their body to revive them.
							if(playerScript.IsGhost)
							{
								message = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Attempted healing {player.Username} but their ghost is outside of their body!";
								Logger.Log(message, Category.Admin);
								LogAdminAction(message);
								return;
							}
							health.RevivePlayerToFullHealth(playerScript);
							playerScript.registerTile.ServerStandUp();
						}
						message = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Healed up Username: {player.Username} ({player.Name})";
					}
					else
					{
						message = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Attempted healing {player.Username} but they had no body!";
					}
					//Log what we did.
					Logger.Log(message, Category.Admin);
					LogAdminAction(message);
				}
			}
		}

		#endregion

		#region Sound

		[Command(requiresAuthority = false)]
		public void CmdPlaySound(string adminId, string adminToken, string index)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			var players = PlayerList.Instance.InGamePlayers;

			if (players == null || players.Count == 0) return; //If list of Players is empty dont run rest of code.

			foreach (var player in players)
			{
				// SoundManager.PlayNetworkedForPlayerAtPos(player.gameObject,
				// player.gameObject.GetComponent<RegisterTile>().WorldPositionClient, index);
			}

			LogAdminAction($"{PlayerList.Instance.GetByUserID(adminId).Username}: played the global sound: {index}.");
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
			if (ProfileManager.runningProfile || ProfileManager.runningMemoryProfile) return;

			string path = Directory.GetCurrentDirectory() + "/Profiles/" + profileName;
			if (File.Exists(path))
			{
				File.Delete(path);
			}

			ProfileMessage.SendToApplicable();
		}

		[Command(requiresAuthority = false)]
		public void CmdStartMemoryProfile(string adminId, string adminToken, bool full)
		{
			if (IsAdmin(adminId, adminToken) == false) return;

			ProfileManager.Instance.RunMemoryProfile(full);
		}

		#endregion

		#region Inventory

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