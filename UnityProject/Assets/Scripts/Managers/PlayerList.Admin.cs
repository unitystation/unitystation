using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SecureStuff;
using DatabaseAPI;
using Mirror;
using UnityEngine;
using DiscordWebhook;
using Lobby;
using Logs;
using Messages.Client;
using Messages.Server;
using Messages.Server.AdminTools;
using Newtonsoft.Json;
using UI;


/// <summary>
/// Admin Controller for players
/// </summary>
public partial class PlayerList
{
	private HashSet<string> serverAdmins = new HashSet<string>();

	public  HashSet<string> ServerAdmins => serverAdmins;

	private HashSet<string> mentorUsers = new HashSet<string>();
	private Dictionary<string, string> loggedInAdmins = new Dictionary<string, string>();
	private Dictionary<string, string> loggedInMentors = new Dictionary<string, string>();
	private BanList banList;
	private string mentorsPath;
	private string adminsPath;
	private string banPath;
	private List<string> whiteListUsers = new List<string>();
	private string whiteListPath;

	private string jobBanPath;
	private JobBanList jobBanList;

	public List<JobBanEntry> clientSideBanEntries = new List<JobBanEntry>();

	public string AdminToken { get; private set; }

	//does the client think he's an admin
	public bool IsClientAdmin;
	public string MentorToken { get; private set; }

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine( AccessFile.AdminFolder, "admins.txt");
		mentorsPath = Path.Combine( AccessFile.AdminFolder, "mentors.txt");
		banPath = Path.Combine( AccessFile.AdminFolder, "banlist.json");
		whiteListPath = Path.Combine( AccessFile.AdminFolder, "whitelist.txt");
		jobBanPath = Path.Combine( AccessFile.AdminFolder, "jobBanlist.json");

		if (AccessFile.Exists(banPath)  == false)
		{
			AccessFile.Save(banPath, JsonConvert.SerializeObject(new BanList()));
		}

		if (AccessFile.Exists(jobBanPath) == false)
		{
			AccessFile.Save(jobBanPath, JsonConvert.SerializeObject(new JobBanList()));
		}

		AccessFile.Watch(adminsPath, ThreadLoadCurrentAdmins);
		AccessFile.Watch(mentorsPath, ThreadLoadCurrentMentors);
		AccessFile.Watch(whiteListPath, ThreadLoadWhiteList);


		LoadBanList();
		LoadCurrentAdmins();
		LoadCurrentMentors();
		LoadWhiteList();
		LoadJobBanList();
	}

	static void LoadBanList()
	{
		Instance.StartCoroutine(LoadBans());
	}


	static void LoadWhiteList()
	{
		Instance.StartCoroutine(LoadWhiteListed());
	}

	static void ThreadLoadWhiteList()
	{
		Thread.Sleep(100);
		Instance.whiteListUsers.Clear();
		Instance.whiteListUsers = new List<string>(AccessFile.ReadAllLines(Instance.whiteListPath));
	}


	static void LoadCurrentAdmins()
	{
		Instance.StartCoroutine(LoadAdmins());
	}

	static void ThreadLoadCurrentAdmins()
	{
		Thread.Sleep(100);
		Instance.serverAdmins.Clear();
		Instance.serverAdmins = new HashSet<string>(AccessFile.ReadAllLines(Instance.adminsPath));
	}


	static void LoadCurrentMentors()
	{

		Instance.StartCoroutine(LoadMentors());
	}

	static void ThreadLoadCurrentMentors()
	{
		Thread.Sleep(100);
		Instance.mentorUsers.Clear();
		Instance.mentorUsers = new HashSet<string>(AccessFile.ReadAllLines(Instance.mentorsPath));
	}

	static void LoadJobBanList()
	{
		Instance.StartCoroutine(LoadJobBans());
	}

	static IEnumerator LoadJobBans()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		Instance.jobBanList = JsonConvert.DeserializeObject<JobBanList>(AccessFile.Load(Instance.jobBanPath));
	}

	static IEnumerator LoadBans()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		Instance.banList = JsonConvert.DeserializeObject<BanList>(AccessFile.Load(Instance.banPath));
	}

	static IEnumerator LoadWhiteListed()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		Instance.whiteListUsers.Clear();
		Instance.whiteListUsers = new List<string>(AccessFile.ReadAllLines(Instance.whiteListPath));
	}

	static IEnumerator LoadAdmins()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		Instance.serverAdmins.Clear();
		Instance.serverAdmins = new HashSet<string>(AccessFile.ReadAllLines(Instance.adminsPath));
	}

	static IEnumerator LoadMentors()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		Instance.mentorUsers.Clear();
		Instance.mentorUsers = new HashSet<string>(AccessFile.ReadAllLines(Instance.mentorsPath));
	}

	[Server]
	public GameObject GetAdmin(string userID, string token)
	{

		if (string.IsNullOrEmpty(userID))
		{
			//allow null admin when doing offline testing
			if (GameData.Instance.OfflineMode)
			{
				return PlayerManager.LocalPlayerObject;
			}
			Loggy.LogError("The User ID for Admin is null!", Category.Admin);
			if (string.IsNullOrEmpty(token))
			{
				Loggy.LogError("The AdminToken value is null!", Category.Admin);
			}

			return null;
		}

		if (!loggedInAdmins.ContainsKey(userID)) return null;

		if (loggedInAdmins[userID] != token) return null;

		TryGetOnlineByUserID(userID, out var admin);
		return admin?.GameObject;
	}

	[Server]
	public List<PlayerInfo> GetAllAdmins()
	{
		var admins = new List<PlayerInfo>();
		foreach (var a in loggedInAdmins)
		{
			if (TryGetOnlineByUserID(a.Key, out var admin))
			{
				admins.Add(admin);
			}
		}

		return admins;
	}

	[Server]
	public bool IsAdmin(string userID)
	{
		return serverAdmins.Contains(userID);
	}

	[Server]
	public GameObject GetMentor(string userID, string token)
	{

		if (string.IsNullOrEmpty(userID))
		{
			//allow null mentor when doing offline testing
			if (GameData.Instance.OfflineMode)
			{
				return PlayerManager.LocalPlayerObject;
			}
			Loggy.LogError("The User ID for Mentor is null!", Category.Mentor);
			if (string.IsNullOrEmpty(token))
			{
				Loggy.LogError("The AdminToken value is null!", Category.Mentor);
			}

			return null;
		}

		if (!loggedInMentors.ContainsKey(userID)) return null;

		if (loggedInMentors[userID] != token) return null;

		TryGetOnlineByUserID(userID, out var admin);
		return admin?.GameObject;
	}

	[Server]
	public List<PlayerInfo> GetAllMentors()
	{
		List<PlayerInfo> mentors = new List<PlayerInfo>();
		foreach (var a in loggedInMentors)
		{
			if (TryGetOnlineByUserID(a.Key, out var mentor))
			{
				mentors.Add(mentor);
			}
		}

		return mentors;
	}

	[Server]
	public bool IsMentor(string userID)
	{
		return mentorUsers.Contains(userID);
	}

	[Server]
	public void TryAddMentor(string userID, bool addToFile = true)
	{
		if (IsMentor(userID) && addToFile == false) return;

		mentorUsers.Add(userID);

		if (TryGetOnlineByUserID(userID, out var player))
		{
			CheckMentorState(player, userID);
		}

		if (addToFile == false) return;

		//Read file to see if already in file
		var fileContents = AccessFile.ReadAllLines(mentorsPath);
		if(fileContents.Contains(userID)) return;

		//Write to file if not
		var newContents = fileContents.Append(userID).ToArray();
		AccessFile.WriteAllLines(mentorsPath, newContents);
	}

	[Server]
	public void TryRemoveMentor(string userID)
	{
		if (IsMentor(userID) == false) return;

		mentorUsers.Remove(userID);

		if (TryGetOnlineByUserID(userID, out var player))
		{
			MentorEnableMessage.Send(player.Connection, string.Empty, false);

			CheckForLoggedOffMentor(userID, player.Username);
		}

		//Read file to see if already in file
		var fileContents = AccessFile.ReadAllLines(mentorsPath);
		if(fileContents.Contains(userID) == false) return;

		//Remove from file if they are in there
		var newContents = fileContents.Where(line => line != userID).ToArray();
		AccessFile.WriteAllLines(mentorsPath, newContents);
	}

	[TargetRpc]
	public void RpcShowCharacterCreatorScreenRemotely(NetworkConnection target)
	{
		LobbyManager.Instance.SetActive(true);
		LobbyManager.Instance.ShowCharacterEditor();
	}

	#region Login

	public bool TryLogIn(PlayerInfo player)
	{
		// Check if the player is considered a server admin
		// Admins can bypass certain checks, like player capacity and multikeying
		if (ValidatePlayerAdminStatus(player))
		{
			CheckAdminState(player);
			Loggy.Log($"Admin {player.Username} (user ID '{player.UserId}') logged in successfully.", Category.Admin);
			return true;
		}

		if (CanRegularPlayerJoin(player) == false) return false;
		if (ValidateMultikeying(player) == false) return false;

		Loggy.Log($"{player.Username} (user ID '{player.UserId}') logged in successfully.", Category.Admin);
		return true;
	}

	private bool ValidatePlayerAdminStatus(PlayerInfo player)
	{
		// Server host instances are always admins
		if (player.UserId == ServerData.UserID)
		{
			serverAdmins.Add(player.UserId);
		}

		// Players are always admins if in offline mode, for testing
		if (GameData.Instance.OfflineMode)
		{
			Loggy.Log($"{player.Username} logged in successfully in offline mode. userid: {player.UserId}", Category.Admin);
			serverAdmins.Add(player.UserId);
		}

		return serverAdmins.Contains(player.UserId);
	}

	private bool CanRegularPlayerJoin(PlayerInfo player)
	{
		if (player.IsAdmin) return true;

		//PlayerLimit Checking:
		//Deny player joining if limit reached and this player wasn't already in the round (in case of disconnect)
		var playerLimit = GameManager.Instance.PlayerLimit;
		if (ConnectionCount > GameManager.Instance.PlayerLimit && roundPlayers.Contains(player) == false)
		{
			ServerKickPlayer(player, $"Server Error: The server is full, player limit: {playerLimit}.");
			Loggy.Log($"{player.Username} tried to log in but PlayerLimit ({playerLimit}) was reached. IP: {player.ConnectionIP}", Category.Admin);
			return false;
		}

		//Whitelist checking:
		//Checks whether the userid is in either the Admins or whitelist AND that the whitelist file has something in it.
		//Whitelist only activates if whitelist is populated.
		var lines = AccessFile.ReadAllLines(whiteListPath);
		if (lines.Length > 0 && !whiteListUsers.Contains(player.UserId))
		{
			ServerKickPlayer(player, $"This server uses a whitelist. This account is not whitelisted.");
			Loggy.Log($"{player.Username} tried to log in but the account is not whitelisted. IP: {player.ConnectionIP}", Category.Admin);
			return false;
		}

		//Banlist checking:
		var banEntry = banList?.CheckForEntry(player.UserId, player.ConnectionIP, player.ClientId);
		if (banEntry != null)
		{
			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if (totalMins > (float)banEntry.minutes)
			{
				//Old ban, remove it
				banList.banEntries.Remove(banEntry);
				SaveBanList();
				Loggy.Log($"{player.Username} ban has expired and the user has logged back in.", Category.Admin);
			}
			else
			{
				//User is still banned:
				ServerKickPlayer(player, $"This account is banned. You were banned for {banEntry.reason}."
						+ $"This ban has {banEntry.minutes - totalMins} minutes remaining.");
				Loggy.Log($"{player.Username} tried to log back in but the account is banned. IP: {player.ConnectionIP}", Category.Admin);
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// Check if the player is logging in with multiple clients or connections.
	/// </summary>
	/// <returns>True if not multikeying</returns>
	private bool ValidateMultikeying(PlayerInfo player)
	{
		//Check if they are already logged in, skip this check if offline mode is enable or if not a release build.
		if (BuildPreferences.isForRelease == false) return true;

		if (TryGetOnlineByUserID(player.UserId, out var existingPlayer)
				&& existingPlayer.Connection != player.Connection)
		{
			InfoWindowMessage.Send(player.GameObject,
					"You were already logged in from another client. That client has been logged out.", "Multikeying");

			ServerKickPlayer(existingPlayer, $"You have logged in from another client. This old client has been disconnected.", announce: false);
			Loggy.Log($"A user tried to connect with another client while already logged in \r\n" +
						$"Details: Username: {player.Username}, ClientID: {player.ClientId}, IP: {player.ConnectionIP}",
				Category.Admin);
		}

		existingPlayer = GetOnline(player.Connection);
		if (existingPlayer != null)
		{
			ServerKickPlayer(player, $"Server Error: You already have an existing connection with the server!");
			Loggy.LogWarning($"Warning 2 simultaneous connections from same IP detected\r\n" +
						$"Details: Unverified Username: {player.Username}, Unverified ClientID: {player.ClientId}, IP: {player.ConnectionIP}",
				Category.Admin);
			return false;
		}

		return true;
	}

	#endregion

	private void SaveBanList()
	{
		AccessFile.Save(banPath, JsonConvert.SerializeObject(banList));
	}

	#region JobBans

	/// <summary>Checks the job ban state of the given player for the given job.</summary>
	/// <returns>True if banned.</returns>
	public bool IsJobBanned(string userID, JobType jobType)
	{
		//jobbanlist checking:
		var jobBanEntry = FindPlayerJobBanEntryServer(userID, jobType);

		// If no entry, then not banned.
		return jobBanEntry != null;
	}

	public JobBanEntry FindPlayerJobBanEntryServer(string userID, JobType jobType, bool serverSideCheck = false)
	{
		if (TryGetByUserID(userID, out var player) == false) return null;

		return FindPlayerJobBanEntry(player, jobType, serverSideCheck);
	}

	/// <summary>
	/// Find the players job ban entry if it exists.
	/// </summary>
	/// <param name="connPlayer"></param>
	/// <param name="jobType"></param>
	/// <param name="serverSideCheck">Used for after round start selecting.</param>
	/// <returns></returns>
	public JobBanEntry FindPlayerJobBanEntry(PlayerInfo connPlayer, JobType jobType, bool serverSideCheck)
	{
		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.ConnectionIP, connPlayer.ClientId);

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			//No job bans at all
			return null;
		}

		var jobBanEntry = jobBanPlayerEntry.Value.Item1.CheckForSpecificJob(jobType);

		if (jobBanEntry == null)
		{
			//Specific Job isnt banned
			return null;
		}

		if (jobBanEntry.isPerma)
		{
			//Specific job has been perma banned
			return jobBanEntry;
		}

		var entryTime = DateTime.ParseExact(jobBanEntry.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
		var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
		if (totalMins > (float)jobBanEntry.minutes)
		{
			JobBanExpireCheck(jobBanEntry, jobBanPlayerEntry.Value.Item2, connPlayer);
		}
		else
		{
			if (!serverSideCheck) return jobBanEntry;

			//User is still banned and has bypassed join data client check!!!!:
			Loggy.Log($"{connPlayer.Username} has bypassed the client side check for ban entry, possible hack attempt. " + $"IP: {connPlayer.ConnectionIP}", Category.Admin);
			return jobBanEntry;
		}

		//Job has been removed as time elapsed
		return null;
	}

	/// <summary>
	/// Called on client join to server, to update their info.
	/// </summary>
	/// <param name="connPlayer"></param>
	/// <returns></returns>
	public List<JobBanEntry> ClientAskingAboutJobBans(PlayerInfo connPlayer)
	{
		if (connPlayer.Equals(PlayerInfo.Invalid))
		{
			Loggy.LogError($"Attempted to check job-ban for invalid player.", Category.Jobs);
			return default;
		}

		string playerUserID = connPlayer.UserId;
		string playerAddress = connPlayer.ConnectionIP;
		string playerClientID = connPlayer.ClientId;

		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(playerUserID, playerAddress, playerClientID);

		if (jobBanPlayerEntry == null)
		{
			//No job bans at all
			return null;
		}

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			//No job bans at all
			return null;
		}

		var index = jobBanPlayerEntry.Value.Item2;

		//Check each job to see if expired
		foreach (var jobBan in jobBanPlayerEntry.Value.Item1.jobBanEntry.ToArray())
		{
			var entryTime = DateTime.ParseExact(jobBan.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if (totalMins > (float)jobBan.minutes && !jobBan.isPerma)
			{
				JobBanExpireCheck(jobBan, jobBanPlayerEntry.Value.Item2, connPlayer);
			}

			if (jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.ConnectionIP, connPlayer.ClientId).Item1.jobBanEntry.Count == 0) break;
		}

		var newJobBanPlayerEntry = jobBanList
			?.CheckForEntry(connPlayer.UserId, connPlayer.ConnectionIP, connPlayer.ClientId).Item1.jobBanEntry;

		if (newJobBanPlayerEntry == null)
		{
			//No job bans at all
			return null;
		}

		if (newJobBanPlayerEntry.Count == 0)
		{
			//If theres now no entries delete player entry completely
			jobBanList.jobBanEntries.Remove(jobBanPlayerEntry.Value.Item1);
			SaveJobBanList();

			//No job bans at all
			return null;
		}

		return newJobBanPlayerEntry;
	}

	public List<JobBanEntry> ListOfBanEntries(string playerID)
	{
		if (TryGetByUserID(playerID, out var player) == false) return null;

		return ClientAskingAboutJobBans(player);
	}

	private void JobBanExpireCheck(JobBanEntry jobBanEntry, int index, PlayerInfo connPlayer)
	{
		//Old ban, remove it
		jobBanList.jobBanEntries[index].jobBanEntry.Remove(jobBanEntry);
		SaveJobBanList();
		Loggy.Log($"{connPlayer.Username} job ban for {jobBanEntry.job} has expired.", Category.Admin);
	}

	void SaveJobBanList()
	{
		AccessFile.Save(jobBanPath, JsonConvert.SerializeObject(jobBanList));
	}

	/// <summary>
	/// Client Side check
	/// </summary>
	/// <param name="job"></param>
	/// <returns></returns>
	public bool ClientJobBanCheck(JobType job)
	{
		if (Instance.clientSideBanEntries == null || Instance.clientSideBanEntries.Count == 0)
		{
			return true;
		}

		foreach (var banEntry in PlayerList.Instance.clientSideBanEntries)
		{
			if (banEntry.job != job) continue;

			if (banEntry.isPerma)
			{
				//Perma Banned
				return false;
			}

			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

			if (totalMins < (float)banEntry.minutes)
			{
				//Time not up yet.
				return false;
			}

			//Is unbanned so try
			break;
		}

		return true;
	}

	/// <summary>
	/// Client Side check
	/// </summary>
	/// <param name="job"></param>
	/// <returns></returns>
	public JobBanEntry ClientCheckBanReturn(JobType job)
	{
		if (Instance.clientSideBanEntries == null || Instance.clientSideBanEntries.Count == 0)
		{
			return null;
		}

		foreach (var banEntry in PlayerList.Instance.clientSideBanEntries)
		{
			if (banEntry.job != job) continue;

			if (banEntry.isPerma)
			{
				//Perma Banned
				return banEntry;
			}

			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

			if (totalMins < (float)banEntry.minutes)
			{
				//Time not up yet.
				return banEntry;
			}

			//Is unbanned so try
			break;
		}

		return null;
	}

	#endregion

	#region JobBanNetMessages

	public class ServerSendsJobBanDataMessage : ServerMessage<ServerSendsJobBanDataMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JobBanEntries;
		}

		public override void Process(NetMessage msg)
		{
			//client Stuff here
			PlayerList.Instance.clientSideBanEntries = JsonConvert.DeserializeObject<List<JobBanEntry>>(msg.JobBanEntries);
		}

		public static NetMessage Send(NetworkConnection requestee, List<JobBanEntry> jobBanEntries)
		{
			NetMessage msg = new NetMessage
			{
				JobBanEntries = JsonConvert.SerializeObject(jobBanEntries)
			};

			SendTo(requestee, msg);
			return msg;
		}
	}

	public class RequestJobBan : ClientMessage<RequestJobBan.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerID;
			public string Reason;
			public bool IsPerma;
			public int Minutes;
			public JobType JobType;
			public bool KickAfter;
			public bool GhostAfter;
		}

		public override void Process(NetMessage msg)
		{
			// Server Stuff here

			if (IsFromAdmin() == false) return;

			if (PlayerList.Instance.TryGetByUserID(msg.PlayerID, out var player) == false)
			{
				Loggy.LogError($"Player with user ID '{msg.PlayerID}' not found. Unable to job-ban from {msg.JobType}.", Category.Admin);
				return;
			}

			Instance.ProcessJobBanRequest(
					SentByPlayer, player, msg.Reason,
					msg.IsPerma, msg.Minutes, msg.JobType, msg.KickAfter, msg.GhostAfter);
		}

		public static NetMessage Send(
				string playerID, string reason, bool isPerma, int minutes, JobType jobType, bool kickAfter, bool ghostAfter)
		{
			NetMessage msg = new NetMessage
			{
				PlayerID = playerID,
				Reason = reason,
				IsPerma = isPerma,
				Minutes = minutes,
				JobType = jobType,
				KickAfter = kickAfter,
				GhostAfter = ghostAfter
			};

			Send(msg);
			return msg;
		}
	}

	#endregion

	#region AdminChecks

	public void CheckAdminState(PlayerInfo player)
	{
		//full admin privs for local offline testing for host player
		if (serverAdmins.Contains(player.UserId) || (GameData.Instance.OfflineMode && player.GameObject == PlayerManager.LocalViewerScript.gameObject) || Application.isEditor)
		{
			//This is an admin, send admin notify to the users client
			Loggy.Log($"{player.Username} logged in as Admin. IP: {player.ConnectionIP}", Category.Admin);
			var newToken = Guid.NewGuid().ToString();
			loggedInAdmins[player.UserId] = newToken;
			player.PlayerRoles |= PlayerRole.Admin;
			AdminEnableMessage.SendMessage(player, newToken);
		}
	}

	public void CheckMentorState(PlayerInfo playerConn, string userid)
	{
		if (mentorUsers.Contains(userid) && !serverAdmins.Contains(userid))
		{
			Loggy.Log($"{playerConn.Username} logged in as Mentor. IP: {playerConn.ConnectionIP}", Category.Admin);
			var newToken = System.Guid.NewGuid().ToString();
			if (!loggedInMentors.ContainsKey(userid))
			{
				loggedInMentors.Add(userid, newToken);
				playerConn.PlayerRoles |= PlayerRole.Mentor;
				MentorEnableMessage.Send(playerConn.Connection, newToken);
			}
		}
	}

	void CheckForLoggedOffAdmin(string userid, string userName)
	{
		if (loggedInAdmins.ContainsKey(userid) == false) return;

		Loggy.Log($"Admin {userName} logged off.", Category.Admin);
		loggedInAdmins.Remove(userid);
	}

	void CheckForLoggedOffMentor(string userid, string userName)
	{
		if (loggedInMentors.ContainsKey(userid) == false) return;

		Loggy.Log($"Mentor {userName} logged off.", Category.Admin);
		loggedInMentors.Remove(userid);
	}

	public void SetClientAsAdmin(string _adminToken)
	{
		AdminToken = _adminToken;
		IsClientAdmin = true;
		ControlTabs.Instance.ToggleOnAdminTab();
		Loggy.Log("You have logged in as an admin. Admin tools are now available.", Category.Admin);
	}

	public void SetClientAsMentor(string _mentorToken)
	{
		MentorToken = _mentorToken;
		Loggy.Log("You have logged in as a mentor. Mentor tools are now available.", Category.Admin);
	}

	public void ProcessAdminEnableRequest(string admin, string userToPromote)
	{
		if (!serverAdmins.Contains(admin)) return;
		if (serverAdmins.Contains(userToPromote)) return;

		Loggy.Log(
			$"{admin} has promoted {userToPromote} to admin. Time: {DateTime.Now}", Category.Admin);

		AccessFile.AppendAllText(adminsPath, "\r\n" + userToPromote);

		serverAdmins.Add(userToPromote);
		if (TryGetOnlineByUserID(userToPromote, out var user) == false) return;

		var newToken = System.Guid.NewGuid().ToString();
		if (!loggedInAdmins.ContainsKey(userToPromote))
		{
			loggedInAdmins.Add(userToPromote, newToken);
			AdminEnableMessage.SendMessage(user, newToken);
		}
	}
	#endregion

	#region Kick/Ban

	public void ServerKickPlayer(PlayerInfo player, string reason, bool announce = true)
	{
		string message = $"A kick is being processed by the server. " +
				$"Username: {player.Username}. Character name: {player.Name}. Processed at: {DateTime.Now}.";
		Loggy.Log(message, Category.Admin);

		StartCoroutine(KickOrBanPlayer(player, reason, false));

		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, $"{message}\nReason: {reason}", "");
		UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(message, null);

		if (announce && ServerData.ServerConfig.DiscordWebhookEnableBanKickAnnouncement)
		{
			message = $"{ServerData.ServerConfig.ServerName}\nPlayer {player.Username} has been banned.";
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");
		}
	}

	public void ServerBanPlayer(PlayerInfo player, string reason, bool announce = true, int minutes = 0)
	{
		string message = $"A ban is being processed by the server. " +
				$"Username: {player.Username}. Character name: {player.Name}. Duration: {minutes} minutes. Processed at: {DateTime.Now}.";
		Loggy.Log(message, Category.Admin);

		StartCoroutine(KickOrBanPlayer(player, reason, true, minutes));

		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message + $"\nReason: {reason}", "");
		UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(message, null);

		if (announce && ServerData.ServerConfig.DiscordWebhookEnableBanKickAnnouncement)
		{
			message = $"{ServerData.ServerConfig.ServerName}\nPlayer {player.Username} has been banned.";
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");
		}
	}

	IEnumerator KickOrBanPlayer(PlayerInfo connPlayer, string reason,
		bool ban = false, int banLengthInMinutes = 0, PlayerInfo adminPlayer = null)
	{
		Loggy.Log("Processing KickPlayer/ban for " + "\n"
				   + "UserId " + connPlayer?.UserId + "\n"
				   + "Username " + connPlayer?.Username + "\n"
				   + "address " + connPlayer?.Connection?.address + "\n"
				   + "clientId " + connPlayer?.ClientId + "\n",
				   Category.Admin);

		string message = "";
		if (ban)
		{
			message = $"You have been banned for {banLengthInMinutes}" +
					  $" minutes. Reason: {reason}";

			int index = banList.banEntries.FindIndex(x => x.userId == connPlayer.UserId);
			if (index != -1)
			{
				Loggy.Log("removing pre-existing ban entry for userId of" + connPlayer.UserId, Category.Admin);
				banList.banEntries.RemoveAt(index);
			}

			banList.banEntries.Add(new BanEntry
			{
				userId = connPlayer?.UserId,
				userName = connPlayer?.Username,
				minutes = banLengthInMinutes,
				reason = reason,
				dateTimeOfBan = DateTime.Now.ToString("O"),
				ipAddress = connPlayer?.Connection?.address,
				clientId = connPlayer?.ClientId,
				adminId = adminPlayer?.UserId,
				adminName = adminPlayer?.Username
			});

			SaveBanList();
			if (banList.banEntries.Count != 0)
			{
				Loggy.Log(banList.banEntries[banList.banEntries.Count - 1].ToString(), Category.Admin);
			}
		}
		else
		{
			message = $"You have been kicked. Reason: {reason}";
		}

		SendClientLogMessage.SendErrorToClient(connPlayer, message, Category.Admin);
		yield return WaitFor.Seconds(0.1f);

		if (connPlayer.Connection == null)
		{
			Loggy.Log($"Not kicking, already disconnected: {connPlayer.Name}", Category.Admin);
			yield break;
		}

		Loggy.Log($"Kicking client {connPlayer.Username} : {message}", Category.Admin);
		InfoWindowMessage.Send(connPlayer.GameObject, message, "Disconnected");

		yield return WaitFor.Seconds(1f);

		Loggy.LogError($"Disconnecting client {connPlayer.Username} : Via admin KickOrBanPlayer {message}", Category.Admin);
		connPlayer.Connection.Disconnect();

		while (!loggedOff.Contains(connPlayer))
		{
			yield return WaitFor.EndOfFrame;
		}

		loggedOff.Remove(connPlayer);
	}

	public void ProcessJobBanRequest(PlayerInfo admin, PlayerInfo player, string reason, bool isPerma, int banMinutes, JobType jobType, bool kickAfter = false, bool ghostAfter = false)
	{
		if (admin.IsAdmin == false) return;

		string message;

		if (isPerma)
		{
			message = $"A job ban has been processed by {admin.Username}: Username: {player.Username} Player: {player.Name} Job: {jobType} IsPerma: {isPerma} Time: {DateTime.Now}";
		}
		else
		{
			message = $"A job ban has been processed by {admin.Username}: Username: {player.Username} Player: {player.Name} Job: {jobType} BanMinutes: {banMinutes} Time: {DateTime.Now}";
		}

		Loggy.Log(message, Category.Admin);

		StartCoroutine(JobBanPlayer(player, reason, isPerma, banMinutes, jobType, admin));

		UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord($"{admin.Username}: job banned {player.Username} from {jobType}, IsPerma: {isPerma}, BanMinutes: {banMinutes}", null);

		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message + $"\nReason: {reason}", "");

		if (ghostAfter)
		{
			if (!player.Script.IsGhost)
			{
				player.Mind.ghostLocked = true;
				player.Mind.Ghost();
			}
		}

		if (kickAfter)
		{
			reason = "Player was kicked after job ban process.";
			StartCoroutine(KickOrBanPlayer(player, reason, false));
			return;
		}

		//Send update if they are still in the game
		if (player.Connection == null) return;
		ServerSendsJobBanDataMessage.Send(player.Connection, ClientAskingAboutJobBans(player));
	}

	IEnumerator JobBanPlayer(PlayerInfo connPlayer, string reason, bool isPermaBool, int banLengthInMinutes, JobType jobType, PlayerInfo admin)
	{
		if (jobBanList == null)
		{
			Loggy.LogError("The job ban list loaded from the json was null, cant add new ban to it.", Category.Admin);
			yield break;
		}

		Loggy.Log("Processing job ban for " + "\n"
													+ "UserId " + connPlayer?.UserId + "\n"
													+ "Username " + connPlayer?.Username + "\n"
													+ "address " + connPlayer?.Connection?.address + "\n"
													+ "clientId " + connPlayer?.ClientId + "\n"
													+ "jobType " + jobType + "\n", Category.Admin
		);

		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.ConnectionIP, connPlayer.ClientId);

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			//Doesnt have a job ban yet

			jobBanList.jobBanEntries.Add(new JobBanPlayerEntry
			{
				userId = connPlayer?.UserId,
				userName = connPlayer?.Username,
				ipAddress = connPlayer?.ConnectionIP,
				clientId = connPlayer?.ClientId,
				jobBanEntry = new List<JobBanEntry>(),
				adminId = admin?.UserId,
				adminName = admin?.Username
			});

			jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.ConnectionIP, connPlayer.ClientId);
		}

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			Loggy.LogError("New job ban list was null even though new one was generated", Category.Admin);
			yield break;
		}

		var jobBanEntry = jobBanPlayerEntry.Value.Item1.CheckForSpecificJob(jobType);

		if (jobBanEntry == null)
		{
			//job ban entries doesnt have job, generate new ban
			jobBanList.jobBanEntries[jobBanPlayerEntry.Value.Item2].jobBanEntry.Add(new JobBanEntry
			{
				job = jobType,
				dateTimeOfBan = DateTime.Now.ToString("O"),
				isPerma = isPermaBool,
				minutes = banLengthInMinutes,
				reason = reason
			});
		}
		else
		{
			//Delete and add new if exists
			jobBanList.jobBanEntries[jobBanPlayerEntry.Value.Item2].jobBanEntry.Remove(jobBanEntry);

			jobBanList.jobBanEntries[jobBanPlayerEntry.Value.Item2].jobBanEntry.Add(new JobBanEntry
			{
				job = jobType,
				dateTimeOfBan = DateTime.Now.ToString("O"),
				isPerma = isPermaBool,
				minutes = banLengthInMinutes,
				reason = reason
			});
		}

		SaveJobBanList();
	}
	#endregion
}

[Serializable]
public class BanList
{
	public List<BanEntry> banEntries = new List<BanEntry>();

	public BanEntry CheckForEntry(string userId, string ipAddress, string clientId)
	{
		var index = banEntries.FindIndex(x => x.userId == userId
											  || x.ipAddress == ipAddress
											  || x.clientId == clientId);
		if (index != -1)
		{
			return banEntries[index];
		}

		return null;
	}
}
[Serializable]
public class BanEntry
{
	public string userId;
	public string userName;
	public double minutes;
	public string dateTimeOfBan;
	public string reason;
	public string ipAddress;
	public string clientId;

	public string adminId;

	public string adminName;

	public override string ToString()
	{
		return ("BanEntry of " + userId + " / " + userName + "\n"
				 + "for " + minutes + " minutes, on" + dateTimeOfBan + "\n"
				 + "on IP " + ipAddress + "\n"
				 + "clientId " + clientId + "\n"
				 + "by " + adminId + " / " + adminName + "\n"
				 + "for reason" + reason);
	}
}

[Serializable]
public class JobBanList
{
	public List<JobBanPlayerEntry> jobBanEntries = new List<JobBanPlayerEntry>();

	public (JobBanPlayerEntry, int) CheckForEntry(string userId, string ipAddress, string clientId)
	{
		var index = jobBanEntries.FindIndex(x => x.userId == userId
											  || x.ipAddress == ipAddress
											  || x.clientId == clientId);
		if (index != -1)
		{
			return (jobBanEntries[index], index);
		}

		return (null, 0);
	}
}
[Serializable]
public class JobBanPlayerEntry
{
	public string userId;
	public string userName;
	public string ipAddress;
	public string clientId;

	public string adminId;

	public string adminName;

	public List<JobBanEntry> jobBanEntry = new List<JobBanEntry>();

	public JobBanEntry CheckForSpecificJob(JobType jobType)
	{
		var index = jobBanEntry.FindIndex(x => x.job == jobType);
		if (index != -1)
		{
			return jobBanEntry[index];
		}

		return null;
	}
}

[Serializable]
public class JobBanEntry
{
	public JobType job;
	public bool isPerma;
	public double minutes;
	public string dateTimeOfBan;
	public string reason;
}
