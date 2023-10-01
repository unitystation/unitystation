using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AddressableReferences;
using UnityEngine;
using Mirror;
using DiscordWebhook;
using InGameEvents;
using Managers;
using Messages.Server;
using Messages.Server.AdminTools;
using Strings;
using HealthV2;
using AdminTools;
using Audio.Containers;
using DatabaseAPI;
using Doors;
using Doors.Modules;
using Logs;
using Objects;
using Objects.Atmospherics;
using Objects.Disposals;
using Objects.Lighting;
using Objects.Wallmounts;
using ScriptableObjects;
using SecureStuff;
using Systems.Atmospherics;
using Systems.Cargo;
using Systems.Electricity;
using Systems.Pipes;
using TileManagement;
using Tiles;
using UI.Systems.AdminTools;
using UI.Systems.AdminTools.DevTools;

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
		public static bool IsAdmin(NetworkConnection sender, out PlayerInfo player, bool logFailure = true)
		{
			player = PlayerList.Instance.GetOnline(sender);
			if (player.IsAdmin == false)
			{
				if (logFailure)
				{
					var message =
						$"Failed Admin check with id: {player?.ClientId}, associated player with that id (null if not valid id): {player?.Username}," +
						$"Possible hacked client with ip address: {sender?.identity?.connectionToClient?.address}, netIdentity object name: {sender?.identity.OrNull()?.name}]";
					Loggy.LogError(message, Category.Exploits);
					LogAdminAction(message);
				}

				return false;
			}

			return true;
		}

		#region Server Settings

		[Command(requiresAuthority = false)]
		public void CmdChangePlayerLimit(int newLimit, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			if (newLimit < 0) return;

			var currentLimit = GameManager.Instance.PlayerLimit;
			if(currentLimit == newLimit) return;

			LogAdminAction($"{player.Username}: Set PlayerLimit to {newLimit} from {currentLimit}");

			GameManager.Instance.PlayerLimit = newLimit;
		}

		//Limit to 5 fps minimum
		public const int MINIUM_SERVER_FRAMERATE = 5;

		[Command(requiresAuthority = false)]
		public void CmdChangeFrameRate(int newLimit, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			if (newLimit < MINIUM_SERVER_FRAMERATE) return;

			var currentLimit = Application.targetFrameRate;
			if(currentLimit == newLimit) return;

			LogAdminAction($"{player.Username}: Set MaxServerFrameRate to {newLimit} from {currentLimit}");

			Application.targetFrameRate = newLimit;
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeServerPassword(string newPassword, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			LogAdminAction($"{player.Username}: Set the Server Password to {newPassword} from {ServerData.ServerConfig.ConnectionPassword}");

			ServerData.ServerConfig.ConnectionPassword = newPassword;
		}

		#endregion

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

		[Command(requiresAuthority = false)]
		public void CmdMake3D(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;
			var message = new StringBuilder();
			message.AppendLine($"{player.Username}: Change the server to 3D");

			Manager3D.Instance.ConvertTo3D();

			if(message.Length == 0) return;
			LogAdminAction(message.ToString());
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeGameMode(string nextGameMode, bool isSecret, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var player) == false) return;

			var message = new StringBuilder();

			if (GameManager.Instance.NextGameMode != nextGameMode)
			{
				message.AppendLine($"{player.Username}: Updated the next game mode with {nextGameMode}");
				GameManager.Instance.NextGameMode = nextGameMode;
			}

			if (GameManager.Instance.SecretGameMode != isSecret)
			{
				message.AppendLine($"{player.Username}: Set the IsSecret GameMode flag to {isSecret}");
				GameManager.Instance.SecretGameMode = isSecret;
			}

			if(message.Length == 0) return;
			LogAdminAction(message.ToString());
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
				if (SubsystemMatrixQueueInit.InitializedAll == false || SubSceneManager.Instance.ServerInitialLoadingComplete == false)
				{
					Chat.AddGameWideSystemMsgToChat($"<color={AdminActionChatColor}> An Admin tried to start the game early but the server wasn't ready. **insert Walter White Breaks Down meme here** </color>");
					return;
				}


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
				CentComm.MakeShuttleCallAnnouncement(shuttle.InitialTimerSeconds, text, true);

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

		#region Player Commands

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

			if (PlayerList.Instance.TryGetByUserID(userToSmite, out var player) == false)
			{
				Loggy.LogError($"{admin.Username} tried to smite a player with user ID '{userToSmite}' but they couldn't be found.");
				return;
			}

			if (player?.Script == null || player.Script.IsGhost) return;

			string message = $"{admin.Username}: Smited Username: {player.Username} ({player.Name})";

			LogAdminAction(message);

			player.Script.GetComponent<IGib>()?.OnGib();

			Chat.AddExamineMsgFromServer(player.Script.gameObject, "You are struck down by a mysterious force!");
		}

		/// <summary>
		/// Heals a player up
		/// </summary>
		[Command(requiresAuthority = false)]
		public void CmdHealUpPlayer(string userToHeal, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			if (PlayerList.Instance.TryGetByUserID(userToHeal, out var player) == false)
			{
				Loggy.LogError($"Could not find player with user ID '{userToHeal}'. Unable to heal.", Category.Admin);
				return;
			}

			//get player stuff.
			PlayerScript playerScript = player.Script;
			Mind playerMind = playerScript.Mind;
			var playerBody = playerMind.Body;
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
			LogAdminAction(message);
		}

		/// <summary>
		/// OOC mute / unmute a player
		/// </summary>
		[Command(requiresAuthority = false)]
		public void CmdOOCMutePlayer(string userToMute, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			if (PlayerList.Instance.TryGetByUserID(userToMute, out var player) == false)
			{
				Loggy.LogError($"Could not find player with user ID '{userToMute}'. Unable to OOC mute.", Category.Admin);
				return;
			}

			player.IsOOCMuted = !player.IsOOCMuted;

			var message = $"{admin.Username}: OOC {(player.IsOOCMuted ? "Muted" : "Unmuted")} {player.Username}";

			//Log what we did.
			LogAdminAction(message);
		}

		/// <summary>
		/// Gives item to player
		/// </summary>
		[Command(requiresAuthority = false)]
		public void CmdGivePlayerItem(string userToGiveItem, string itemPrefabName, int count, string customMessage, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			if (PlayerList.Instance.TryGetByUserID(userToGiveItem, out var player) == false)
			{
				Loggy.LogError($"Could not find player with user ID '{userToGiveItem}'. Unable to give item.", Category.Admin);
				return;
			}

			if (player.Script.OrNull()?.DynamicItemStorage == null)
			{
				Loggy.LogError($"No DynamicItemStorage on '{player.Name}'. Unable to give item.", Category.Admin);
				return;
			}

			var item = Spawn.ServerPrefab(itemPrefabName, player.Mind.Body.gameObject.AssumedWorldPosServer());
			var slot = player.Script.DynamicItemStorage.GetBestHandOrSlotFor(item.GameObject);
			if (item.GameObject.TryGetComponent<Stackable>(out var stackable) && stackable.MaxAmount <= count)
			{
				stackable.ServerSetAmount(count);
			}

			if (slot != null)
			{
				Inventory.ServerAdd(item.GameObject, slot);
			}

			if (string.IsNullOrEmpty(customMessage) == false)
			{
				Chat.AddExamineMsg(player.GameObject, customMessage);
			}

			Chat.AddExamineMsg(admin.GameObject, $"You have given {player.Script.playerName} : {item.GameObject.ExpensiveName()}");

			var message = $"{admin.Username}: gave {player.Script.playerName} {count} {item.GameObject.ExpensiveName()}";

			//Log what we did.
			LogAdminAction(message);
		}

		#endregion

		#region Sound

		//FIXME: DISABLED UNTIL JUSTIN RETURNS WORK ON THIS AND WEAVER ISSUES GET FIXED

		[Command(requiresAuthority = false)]
		public void CmdPlaySound(string addressableAudioSource, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			AddressableAudioSource sound = new AddressableAudioSource();
			sound.AssetAddress = addressableAudioSource;
			SoundManager.PlayNetworked(sound);
		}


		#endregion

		#region Music

		[Command(requiresAuthority = false)]
		public void CmdPlayMusic(string addressableAudioSource, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			AddressableAudioSource sound = new AddressableAudioSource();
			sound.AssetAddress = addressableAudioSource;
			MusicManager.PlayNetworked(sound);
		}

		#endregion

		#region Profiling

		[Command(requiresAuthority = false)]
		public void CmdStartProfile(int frameCount, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out _) == false) return;

			SafeProfileManager.Instance.StartProfile(frameCount);
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
			if (SafeProfileManager.runningProfile || SafeProfileManager.runningMemoryProfile) return;

			SafeProfileManager.Instance.RemoveProfile(profileName);

			ProfileMessage.SendToApplicable();
		}

		[Command(requiresAuthority = false)]
		public void CmdStartMemoryProfile(bool full, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out _) == false) return;

			SafeProfileManager.Instance.RunMemoryProfile(full);
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

		#region Mentor

		[Command(requiresAuthority = false)]
		public void CmdAddMentor(string userToUpgrade, bool isPermanent, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			if (PlayerList.Instance.IsMentor(userToUpgrade)) return;

			PlayerList.Instance.TryAddMentor(userToUpgrade, isPermanent);

			if (PlayerList.Instance.TryGetByUserID(userToUpgrade, out var player) == false)
			{
				Loggy.LogWarning($"{admin.Username} has set user with ID '{userToUpgrade}' "
						+ "as mentor but they could not be found!", Category.Admin);
				return;
			}

			LogAdminAction($"{admin.Username}: Gave {player.Username} {(isPermanent ? "permanent" : "temporary")} Mentor");
		}

		[Command(requiresAuthority = false)]
		public void CmdRemoveMentor(string userToDowngrade, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			PlayerList.Instance.TryRemoveMentor(userToDowngrade);

			if (PlayerList.Instance.TryGetByUserID(userToDowngrade, out var player) == false)
			{
				Loggy.LogWarning($"{admin.Username} has unset user with ID '{userToDowngrade}' "
						+ "as mentor but they could not be found!", Category.Admin);
				return;
			}

			LogAdminAction($"{admin.Username}: Removed {player.Username} mentor");
		}

		#endregion

		#region LogAdminAction

		public static void LogAdminAction(string msg, string userName = "")
		{
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				userName);
			Loggy.Log(msg, Category.Admin);
		}

		#endregion

		#region CargoControlCommands

		[Command(requiresAuthority = false)]
		public void CmdRemoveBounty(int index, bool completeBounty, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if (CargoManager.Instance.ActiveBounties.Count <= index) return;
			if (completeBounty)
			{
				CargoManager.Instance.CompleteBounty(CargoManager.Instance.ActiveBounties[index]);
				CargoManager.Instance.OnCreditsUpdate?.Invoke();
				CargoManager.Instance.OnBountiesUpdate?.Invoke();
				return;
			}

			CargoManager.Instance.ActiveBounties.Remove(CargoManager.Instance.ActiveBounties[index]);
			CargoManager.Instance.OnBountiesUpdate?.Invoke();
		}

		[Command(requiresAuthority = false)]
		public void CmdAdjustBountyRewards(int index, int newReward, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			CargoManager.Instance.ActiveBounties[index].Reward = newReward;
			CargoManager.Instance.OnBountiesUpdate?.Invoke();
		}

		[TargetRpc]
		private void TargetSendCargoData(NetworkConnection target, List<CargoManager.BountySyncData> data)
		{
			AdminBountyManager.Instance.RefreshBountiesList(data);
		}

		[TargetRpc]
		private void TargetUpdateBudgetForClient(NetworkConnection target, int data)
		{
			AdminBountyManager.Instance.budgetInput.text = data.ToString();
		}

		[Command(requiresAuthority = false)]
		public void CmdRequestCargoServerData(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			List<CargoManager.BountySyncData> simpleData = new List<CargoManager.BountySyncData>();
			for (int i = 0; i < CargoManager.Instance.ActiveBounties.Count; i++)
			{
				var foundBounty = new CargoManager.BountySyncData();
				foundBounty.Title = CargoManager.Instance.ActiveBounties[i].Title;
				foundBounty.Reward = CargoManager.Instance.ActiveBounties[i].Reward;
				foundBounty.Desc = CargoManager.Instance.ActiveBounties[i].TooltipDescription;
				foundBounty.Index = i;
				simpleData.Add(foundBounty);
			}
			TargetSendCargoData(sender, simpleData);
			TargetUpdateBudgetForClient(sender, CargoManager.Instance.Credits);
		}

		[Command(requiresAuthority = false)]
		public void CmdAddBounty(ItemTrait trait, int amount, string title, string description, int reward, bool announce, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			CargoManager.Instance.AddBounty(trait, amount, title, description, reward, announce);
			CargoManager.Instance.OnBountiesUpdate?.Invoke();
			LogAdminAction($"{admin.Username} has added a new bounty -> Title : {title} || reward : {reward} || Announce : {announce}");
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeBudget(int budget, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			CargoManager.Instance.Credits = budget;
			CargoManager.Instance.OnCreditsUpdate?.Invoke();
			LogAdminAction($"{admin.Username} has changed the cargo budget to -> {budget}");
		}

		[Command(requiresAuthority = false)]
		public void CmdChangeCargoConnectionStatus(bool online, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			CargoManager.Instance.CargoOffline = online;
			CargoManager.Instance.OnConnectionChangeToCentComm?.Invoke();
			LogAdminAction($"{admin.Username} has changed the cargo online status to -> {online}");
		}

		[Command(requiresAuthority = false)]
		public void CmdToggleCargoRandomBounty(bool state, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			CargoManager.Instance.CargoOffline = state;
			CargoManager.Instance.OnConnectionChangeToCentComm?.Invoke();
			LogAdminAction($"{admin.Username} has changed the cargo random bounties status to -> {state}");
		}

		#endregion

		#region RightClickCommands

		#region Integrity

		[Command(requiresAuthority = false)]
		public void CmdAdminMakeHotspot(GameObject onObject, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if (onObject == null) return;

			var reactionManager = onObject.GetComponentInParent<ReactionManager>();
			if (reactionManager == null) return;

			reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition(), 1000, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.down, 1000, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.left, 1000, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.up, 1000, true);
			reactionManager.ExposeHotspotWorldPosition(onObject.TileWorldPosition() + Vector2Int.right, 1000, true);

			LogAdminAction($"{admin.Username} exposed: {onObject.ExpensiveName()}");
		}

		[Command(requiresAuthority = false)]
		public void CmdAdminSmash(GameObject toSmash, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			if (toSmash == null) return;

			var integrity = toSmash.GetComponent<Integrity>();
			if (integrity == null) return;

			LogAdminAction($"{admin.Username} smashed: {toSmash.ExpensiveName()}");

			integrity.ApplyDamage(float.MaxValue, AttackType.Melee, DamageType.Brute);
		}

		#endregion

		#region AdminOverlay

		[Command(requiresAuthority = false)]
		public void CmdGetAdminOverlayFullUpdate(NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			AdminOverlay.RequestFullUpdate(admin);
		}

		#endregion

		#region Doors

		[Command(requiresAuthority = false)]
		public void CmdOpenDoor(GameObject doorToOpen, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(doorToOpen == null) return;

			if(doorToOpen.TryGetComponent<DoorMasterController>(out var doorMasterController) == false) return;

			//Open no matter what, even if welded or bolted closed
			doorMasterController.Open();

			LogAdminAction($"{admin.Username} forced {doorToOpen.ExpensiveName()} to open");
		}

		[Command(requiresAuthority = false)]
		public void CmdToggleBoltDoor(GameObject doorToToggle, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(doorToToggle == null) return;

			if(doorToToggle.TryGetComponent<DoorMasterController>(out var doorMasterController) == false) return;

			//Toggle bolt state
			var boltModule = doorMasterController.GetComponentInChildren<BoltsModule>();
			boltModule.ToggleBolts();

			LogAdminAction($"{admin.Username} toggled the bolts {(boltModule.BoltsDown ? "ON" : "OFF")} for: {doorToToggle.ExpensiveName()}");
		}

		[Command(requiresAuthority = false)]
		public void CmdToggleElectrifiedDoor(GameObject doorToToggle, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(doorToToggle == null) return;

			if(doorToToggle.TryGetComponent<DoorMasterController>(out var doorMasterController) == false) return;

			//Toggle electrify state
			var electrify = doorMasterController.GetComponentInChildren<ElectrifiedDoorModule>();
			electrify.ToggleElectrocution();

			LogAdminAction($"{admin.Username} toggled electrify {(electrify.IsElectrified ? "ON" : "OFF")} for: {doorToToggle.ExpensiveName()}");
		}

		#endregion

		#region UniversalObjectPhysics

		[Command(requiresAuthority = false)]
		public void CmdTeleportToObject(GameObject teleportTo, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(teleportTo == null) return;

			if(teleportTo.TryGetComponent<UniversalObjectPhysics>(out var uop) == false) return;

			var adminScript = admin.Script;
			if(adminScript == null) return;

			if (adminScript.ObjectPhysics != null)
			{
				adminScript.ObjectPhysics.AppearAtWorldPositionServer(uop.OfficialPosition, false, false);
			}
			else if(adminScript.TryGetComponent<GhostMove>(out var ghostMove))
			{
				ghostMove.ForcePositionClient(uop.OfficialPosition, false);
			}

			LogAdminAction($"{admin.Username} teleported themselves to: {teleportTo.ExpensiveName()}");
		}

		[Command(requiresAuthority = false)]
		public void CmdTogglePushable(GameObject gameObjectToToggle, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(gameObjectToToggle == null) return;

			if(gameObjectToToggle.TryGetComponent<UniversalObjectPhysics>(out var uop) == false) return;

			uop.SetIsNotPushable(!uop.isNotPushable);

			LogAdminAction($"{admin.Username} made {gameObjectToToggle.ExpensiveName()} {(uop.IsNotPushable ? "not" : "")} pushable");
		}

		#endregion

		#region Shuttle

		[Command(requiresAuthority = false)]
		public void CmdEarlyLaunch(GameObject shuttleConsole, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(shuttleConsole == null) return;

			if(shuttleConsole.TryGetComponent<EscapeShuttleConsole>(out var escapeShuttleConsole) == false) return;

			escapeShuttleConsole.DepartShuttle();

			LogAdminAction($"{admin.Username} triggered the early launch on the escape shuttle!");
		}

		#endregion

		#region Buttons

		[Command(requiresAuthority = false)]
		public void CmdActivateButton(GameObject button, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(button == null) return;

			if (button.TryGetComponent<GeneralSwitch>(out var generalSwitch))
			{
				generalSwitch.RunDoorController();
				LogAdminAction($"{admin.Username} activated button : {button.ExpensiveName()}");
				return;
			}

			if(button.TryGetComponent<DoorSwitch>(out var doorSwitch) == false) return;

			doorSwitch.RunDoorController();

			LogAdminAction($"{admin.Username} activated button : {button.ExpensiveName()}");
		}

		#endregion

		#region Health

		/// <summary>
		/// Heals a mob
		/// </summary>
		[Command(requiresAuthority = false)]
		public void CmdHealMob(GameObject mobToHeal, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;
			if(mobToHeal == null) return;

			//Does this player have a body that can be healed?
			if (mobToHeal.TryGetComponent<IFullyHealable>(out var fullyHealable) == false) return;

			fullyHealable.FullyHeal();

			//Log what we did.
			LogAdminAction($"{admin.Username} healed {mobToHeal.ExpensiveName()} to full health");
		}

		#endregion

		#endregion

		#region TilePlacer

		[Command(requiresAuthority = false)]
		public void CmdPlaceTile(GUI_DevTileChanger.PlaceStruct data, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			var matrixInfo = MatrixManager.Get(data.matrixId);
			if (matrixInfo == null || matrixInfo == MatrixInfo.Invalid)
			{
				Chat.AddExamineMsgFromServer(admin, "Invalid matrix!");
				return;
			}

			data.startWorldPosition.z = 0;
			data.endWorldPosition.z = 0;

			var startLocalPos = MatrixManager.WorldToLocalInt(data.startWorldPosition, matrixInfo);
			var endLocalPos = MatrixManager.WorldToLocalInt(data.endWorldPosition, matrixInfo);

			Matrix4x4? matrix4X4 = null;
			if (data.orientation != OrientationEnum.Default && data.orientation != OrientationEnum.Up_By0)
			{
				int offset = PipeFunctions.GetOffsetAngle(Orientation.FromEnum(data.orientation).Degrees);
				Quaternion rot = Quaternion.Euler(0.0f, 0.0f, offset);
				matrix4X4 = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
			}

			if (data.categoryIndex >= TileCategorySO.Instance.TileCategories.Count)
			{
				Chat.AddExamineMsgFromServer(admin, "Invalid categoryIndex!");
				return;
			}

			if (data.tileIndex >= TileCategorySO.Instance.TileCategories[data.categoryIndex].CombinedTileList.Count)
			{
				Chat.AddExamineMsgFromServer(admin, "Invalid tileIndex!");
				return;
			}

			var tile = TileCategorySO.Instance.TileCategories[data.categoryIndex].CombinedTileList[data.tileIndex];

			//Do meta tiles if possible
			if (PlaceMetaTile(tile, matrix4X4, matrixInfo, data.colour, startLocalPos, endLocalPos, admin)) return;

			//If single clicking do only one tile
			if (startLocalPos == endLocalPos)
			{
				PlaceTile(data.colour, tile as LayerTile, matrix4X4, matrixInfo, startLocalPos, admin);
				return;
			}

			//Drag clicking get all positions in and place tiles
			var xMin = startLocalPos.x < endLocalPos.x ? startLocalPos.x : endLocalPos.x;
			var yMin = startLocalPos.y < endLocalPos.y ? startLocalPos.y : endLocalPos.y;

			for (int i = 0; i <= Math.Abs(startLocalPos.x - endLocalPos.x); i++)
			{
				for (int j = 0; j <= Math.Abs(startLocalPos.y - endLocalPos.y); j++)
				{
					var localPos = new Vector3Int(xMin + i, yMin + j);

					PlaceTile(data.colour, tile as LayerTile, matrix4X4, matrixInfo, localPos, admin);
				}
			}
		}

		private bool PlaceMetaTile(GenericTile tile, Matrix4x4? matrix4X4, MatrixInfo matrixInfo, Color? colour,
			Vector3Int startLocalPos, Vector3Int endLocalPos, PlayerInfo admin)
		{
			if (tile == null) return true;

			var metaTile = tile as MetaTile;
			if (metaTile == null) return false;

			if (startLocalPos == endLocalPos)
			{
				foreach (var tileToPlace in metaTile.GetTiles())
				{
					PlaceTile(colour, tileToPlace, matrix4X4, matrixInfo, startLocalPos, admin);
				}

				return true;
			}

			//Drag clicking get all positions in and place tiles
			var xMin = startLocalPos.x < endLocalPos.x ? startLocalPos.x : endLocalPos.x;
			var yMin = startLocalPos.y < endLocalPos.y ? startLocalPos.y : endLocalPos.y;

			for (int i = 0; i <= Math.Abs(startLocalPos.x - endLocalPos.x); i++)
			{
				for (int j = 0; j <= Math.Abs(startLocalPos.y - endLocalPos.y); j++)
				{
					var localPos = new Vector3Int(xMin + i, yMin + j);

					foreach (var tileToPlace in metaTile.GetTiles())
					{
						PlaceTile(colour, tileToPlace, matrix4X4, matrixInfo, localPos, admin);
					}
				}
			}

			return true;
		}

		private void PlaceTile(Color? colour, LayerTile tile, Matrix4x4? matrix4X4, MatrixInfo matrixInfo,
			Vector3Int localPos, PlayerInfo adminInfo)
		{
			if (tile == null)
			{
				Chat.AddExamineMsgFromServer(adminInfo, "Invalid tile!");
				return;
			}

			Vector3Int searchVector;

			if (tile is OverlayTile overlayTile)
			{
				searchVector = matrixInfo.MetaTileMap.AddOverlay(localPos, overlayTile, matrix4X4, colour);
			}
			else
			{
				searchVector = matrixInfo.MetaTileMap.SetTile(localPos, tile, matrix4X4, colour);
			}

			if (tile is ElectricalCableTile electricalCableTile)
			{
				matrixInfo.Matrix.AddElectricalNode(localPos, electricalCableTile);
				ElectricalManager.Instance.electricalSync.StructureChange = true;
				return;
			}

			if (tile is PipeTile pipeTile)
			{
				if (matrix4X4 == null)
				{
					matrix4X4 = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
				}

				pipeTile.InitialiseNodeNew(searchVector, matrixInfo.Matrix, matrix4X4.Value);
				return;
			}

			if (tile is DisposalPipe disposalPipe)
			{
				disposalPipe.InitialiseNode(searchVector, matrixInfo.Matrix);
			}
		}

		[Command(requiresAuthority = false)]
		public void CmdRemoveTile(GUI_DevTileChanger.RemoveStruct data, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			var matrixInfo = MatrixManager.Get(data.matrixId);
			if (matrixInfo == null || matrixInfo == MatrixInfo.Invalid)
			{
				Chat.AddExamineMsgFromServer(admin, "Invalid matrix!");
				return;
			}

			data.startWorldPosition.z = 0;
			data.endWorldPosition.z = 0;

			var startLocalPos = MatrixManager.WorldToLocalInt(data.startWorldPosition, matrixInfo);
			var endLocalPos = MatrixManager.WorldToLocalInt(data.endWorldPosition, matrixInfo);

			//If single clicking do only remove one tile
			if (startLocalPos == endLocalPos)
			{
				RemoveTile(data.layerType, matrixInfo, startLocalPos, data.overlayType);
				return;
			}

			//Drag clicking get all positions in and place tiles
			var xMin = startLocalPos.x < endLocalPos.x ? startLocalPos.x : endLocalPos.x;
			var yMin = startLocalPos.y < endLocalPos.y ? startLocalPos.y : endLocalPos.y;

			for (int i = 0; i <= Math.Abs(startLocalPos.x - endLocalPos.x); i++)
			{
				for (int j = 0; j <= Math.Abs(startLocalPos.y - endLocalPos.y); j++)
				{
					var localPos = new Vector3Int(xMin + i, yMin + j);
					RemoveTile(data.layerType, matrixInfo, localPos, data.overlayType);
				}
			}
		}

		private static void RemoveTile(LayerType layerType, MatrixInfo matrixInfo, Vector3Int startLocalPos, OverlayType overlayType)
		{
			if (overlayType != OverlayType.None)
			{
				matrixInfo.MetaTileMap.RemoveFloorWallOverlaysOfType(startLocalPos, overlayType);
				return;
			}

			matrixInfo.MetaTileMap.RemoveTileWithlayer(startLocalPos, layerType, false);
		}

		[Command(requiresAuthority = false)]
		public void CmdColourTile(GUI_DevTileChanger.ColourStruct data, NetworkConnectionToClient sender = null)
		{
			if (IsAdmin(sender, out var admin) == false) return;

			var matrixInfo = MatrixManager.Get(data.matrixId);
			if (matrixInfo == null || matrixInfo == MatrixInfo.Invalid)
			{
				Chat.AddExamineMsgFromServer(admin, "Invalid matrix!");
				return;
			}

			data.startWorldPosition.z = 0;
			data.endWorldPosition.z = 0;

			var startLocalPos = MatrixManager.WorldToLocalInt(data.startWorldPosition, matrixInfo);
			var endLocalPos = MatrixManager.WorldToLocalInt(data.endWorldPosition, matrixInfo);

			var category = TileCategorySO.Instance.TileCategories[data.categoryIndex];

			//If single clicking do only remove one tile
			if (startLocalPos == endLocalPos)
			{
				ColourTile(matrixInfo, startLocalPos, category.LayerType, data.colour);
				return;
			}

			//Drag clicking get all positions in and place tiles
			var xMin = startLocalPos.x < endLocalPos.x ? startLocalPos.x : endLocalPos.x;
			var yMin = startLocalPos.y < endLocalPos.y ? startLocalPos.y : endLocalPos.y;

			for (int i = 0; i <= Math.Abs(startLocalPos.x - endLocalPos.x); i++)
			{
				for (int j = 0; j <= Math.Abs(startLocalPos.y - endLocalPos.y); j++)
				{
					var localPos = new Vector3Int(xMin + i, yMin + j);
					ColourTile(matrixInfo, localPos, category.LayerType, data.colour);
				}
			}
		}

		private void ColourTile(MatrixInfo matrixInfo, Vector3Int localPos, LayerType layerType, Color? colour)
		{
			matrixInfo.MetaTileMap.SetColour(localPos, layerType, colour);
		}

		#endregion

		#region DebugCommands

		private static IEnumerator KillLights()
		{
			if (MatrixManager.MainStationMatrix?.Objects == null) yield break;

			var currentIndex = 0;
			var maximumIndexes = 20;
			foreach (var stationObject in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<LightSource>())
			{
				if (currentIndex >= maximumIndexes)
				{
					currentIndex = 0;
					yield return WaitFor.EndOfFrame;
				}
				stationObject.Integrity.ForceDestroy();
				currentIndex++;
			}
		}

		private static IEnumerator SelfPowerEverything()
		{
			if (MatrixManager.MainStationMatrix?.Objects == null) yield break;

			var currentIndex = 0;
			var maximumIndexes = 20;
			foreach (var stationObject in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<APCPoweredDevice>())
			{
				if (currentIndex >= maximumIndexes)
				{
					currentIndex = 0;
					yield return WaitFor.EndOfFrame;
				}
				stationObject.ChangeToSelfPowered();
				currentIndex++;
			}
		}

		private static IEnumerator TurnOnAllEmergancyLights()
		{
			if (MatrixManager.MainStationMatrix?.Objects == null) yield break;

			var currentIndex = 0;
			var maximumIndexes = 20;
			foreach (var stationObject in MatrixManager.MainStationMatrix.Objects.GetComponentsInChildren<LightSource>())
			{
				if (currentIndex >= maximumIndexes)
				{
					currentIndex = 0;
					yield return WaitFor.EndOfFrame;
				}
				stationObject.ServerChangeLightState(LightMountState.Emergency);
				currentIndex++;
			}
		}


		[Command(requiresAuthority = false)]
		public void DestroyAllLights(NetworkConnectionToClient conn = null)
		{
			if (IsAdmin(conn, out var player) == false) return;
			StartCoroutine(KillLights());
			Chat.AddSystemMsgToChat(
				"<color=blue>Lights are being destroyed to save energy and spice up the crew-members' working experience.</color>",
				MatrixManager.MainStationMatrix);
		}

		[Command(requiresAuthority = false)]
		public void SelfSuficeAllMachines(NetworkConnectionToClient conn = null)
		{
			if (IsAdmin(conn, out var player) == false) return;
			StartCoroutine(SelfPowerEverything());
			Chat.AddSystemMsgToChat(
				"<color=blue>An admin is updating all machines on the station to not require APCs.</color>",
				MatrixManager.MainStationMatrix);
		}

		[Command(requiresAuthority = false)]
		public void TurnOnEmergencyLightsStationWide(NetworkConnectionToClient conn = null)
		{
			if (IsAdmin(conn, out var player) == false) return;
			StartCoroutine(TurnOnAllEmergancyLights());
			Chat.AddSystemMsgToChat(
				"<color=red>Emergency Lights active.</color>",
				MatrixManager.MainStationMatrix);
		}

		#endregion
	}
}
