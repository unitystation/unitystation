using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using DatabaseAPI;
using Mirror;
using UnityEngine;
using DiscordWebhook;
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
	private FileSystemWatcher adminListWatcher;
	private FileSystemWatcher mentorListWatcher;
	private FileSystemWatcher WhiteListWatcher;
	private List<string> adminUsers = new List<string>();
	private List<string> mentorUsers = new List<string>();
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
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");
		mentorsPath = Path.Combine(Application.streamingAssetsPath, "admin", "mentors.txt");
		banPath = Path.Combine(Application.streamingAssetsPath, "admin", "banlist.json");
		whiteListPath = Path.Combine(Application.streamingAssetsPath, "admin", "whitelist.txt");
		jobBanPath = Path.Combine(Application.streamingAssetsPath, "admin", "jobBanlist.json");

		if (!File.Exists(adminsPath))
		{
			File.CreateText(adminsPath).Close();
		}

		if (!File.Exists(mentorsPath))
		{
			File.CreateText(mentorsPath).Close();
		}

		if (!File.Exists(banPath))
		{
			File.WriteAllText(banPath, JsonUtility.ToJson(new BanList()));
		}

		if (!File.Exists(whiteListPath))
		{
			File.CreateText(whiteListPath).Close();
		}

		if (!File.Exists(jobBanPath))
		{
			File.WriteAllText(jobBanPath, JsonUtility.ToJson(new JobBanList()));
		}

		adminListWatcher = new FileSystemWatcher();
		adminListWatcher.Path = Path.GetDirectoryName(adminsPath);
		adminListWatcher.Filter = Path.GetFileName(adminsPath);
		adminListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		adminListWatcher.Changed += LoadCurrentAdmins;
		adminListWatcher.EnableRaisingEvents = true;

		mentorListWatcher = new FileSystemWatcher();
		mentorListWatcher.Path = Path.GetDirectoryName(mentorsPath);
		mentorListWatcher.Filter = Path.GetFileName(mentorsPath);
		mentorListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		mentorListWatcher.Changed += LoadCurrentMentors;
		mentorListWatcher.EnableRaisingEvents = true;

		WhiteListWatcher = new FileSystemWatcher();
		WhiteListWatcher.Path = Path.GetDirectoryName(whiteListPath);
		WhiteListWatcher.Filter = Path.GetFileName(whiteListPath);
		WhiteListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		WhiteListWatcher.Changed += LoadWhiteList;
		WhiteListWatcher.EnableRaisingEvents = true;

		LoadBanList();
		LoadCurrentAdmins();
		LoadCurrentMentors();
		LoadWhiteList();
		LoadJobBanList();
	}

	void LoadBanList()
	{
		StartCoroutine(LoadBans());
	}
	void LoadWhiteList(object source, FileSystemEventArgs e)
	{
		LoadWhiteList();
	}

	void LoadWhiteList()
	{
		StartCoroutine(LoadWhiteListed());
	}

	void LoadCurrentAdmins(object source, FileSystemEventArgs e)
	{
		LoadCurrentAdmins();
	}

	void LoadCurrentAdmins()
	{
		StartCoroutine(LoadAdmins());
	}

	void LoadCurrentMentors(object source, FileSystemEventArgs e)
	{
		LoadCurrentMentors();
	}

	void LoadCurrentMentors()
	{
		StartCoroutine(LoadMentors());
	}

	void LoadJobBanList()
	{
		StartCoroutine(LoadJobBans());
	}

	IEnumerator LoadJobBans()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		jobBanList = JsonUtility.FromJson<JobBanList>(File.ReadAllText(jobBanPath));
	}

	IEnumerator LoadBans()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		banList = JsonUtility.FromJson<BanList>(File.ReadAllText(banPath));
	}

	IEnumerator LoadWhiteListed()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		whiteListUsers.Clear();
		whiteListUsers = new List<string>(File.ReadAllLines(whiteListPath));
	}

	IEnumerator LoadAdmins()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		adminUsers.Clear();
		adminUsers = new List<string>(File.ReadAllLines(adminsPath));
	}

	IEnumerator LoadMentors()
	{
		//ensure any writing has finished
		yield return WaitFor.EndOfFrame;
		mentorUsers.Clear();
		mentorUsers = new List<string>(File.ReadAllLines(mentorsPath));
	}

	[Server]
	public GameObject GetAdmin(string userID, string token)
	{

		if (string.IsNullOrEmpty(userID))
		{
			//allow null admin when doing offline testing
			if (GameData.Instance.OfflineMode)
			{
				return PlayerManager.LocalPlayer;
			}
			Logger.LogError("The User ID for Admin is null!", Category.Admin);
			if (string.IsNullOrEmpty(token))
			{
				Logger.LogError("The AdminToken value is null!", Category.Admin);
			}

			return null;
		}

		if (!loggedInAdmins.ContainsKey(userID)) return null;

		if (loggedInAdmins[userID] != token) return null;

		return GetByUserID(userID).GameObject;
	}

	[Server]
	public List<ConnectedPlayer> GetAllAdmins()
	{
		var admins = new List<ConnectedPlayer>();
		foreach (var a in loggedInAdmins)
		{
			var getConn = GetByUserID(a.Key);
			if (getConn != null)
			{
				admins.Add(getConn);
			}
		}

		return admins;
	}

	[Server]
	public bool IsAdmin(ConnectedPlayer player)
	{
		return IsAdmin(player.UserId);
	}

	[Server]
	public bool IsAdmin(string userID)
	{
		return adminUsers.Contains(userID);
	}

	[Server]
	public GameObject GetMentor(string userID, string token)
	{

		if (string.IsNullOrEmpty(userID))
		{
			//allow null mentor when doing offline testing
			if (GameData.Instance.OfflineMode)
			{
				return PlayerManager.LocalPlayer;
			}
			Logger.LogError("The User ID for Mentor is null!", Category.Mentor);
			if (string.IsNullOrEmpty(token))
			{
				Logger.LogError("The AdminToken value is null!", Category.Mentor);
			}

			return null;
		}

		if (!loggedInMentors.ContainsKey(userID)) return null;

		if (loggedInMentors[userID] != token) return null;

		return GetByUserID(userID).GameObject;
	}

	[Server]
	public List<ConnectedPlayer> GetAllMentors()
	{
		List<ConnectedPlayer> mentors = new List<ConnectedPlayer>();
		foreach (var a in loggedInMentors)
		{
			var getConn = GetByUserID(a.Key);
			if (getConn != null)
			{
				mentors.Add(getConn);
			}
		}

		return mentors;
	}

	[Server]
	public bool IsMentor(string userID)
	{
		return mentorUsers.Contains(userID);
	}

	public async Task<bool> ValidatePlayer(string unverifiedClientId, string unverifiedUsername,
		string unverifiedUserid, int unverifiedClientVersion, ConnectedPlayer unverifiedConnPlayer,
		string unverifiedToken)
	{
		var validAccount = await CheckUserState(unverifiedUserid, unverifiedToken, unverifiedConnPlayer, unverifiedClientId);

		if (!validAccount)
		{
			return false;
		}

		if (unverifiedClientVersion != GameData.BuildNumber)
		{
			StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Invalid Client Version! You need version {GameData.BuildNumber}." +
												  " This can be acquired through the station hub."));
			return false;
		}

		return true;
	}

	//Check if tokens match and if the player is an admin or is banned
	private async Task<bool> CheckUserState(
		string unverifiedUserid,
		string unverifiedToken,
		ConnectedPlayer unverifiedConnPlayer,
		string unverifiedClientId)
	{
		if (GameData.Instance.OfflineMode)
		{
			Logger.Log($"{unverifiedConnPlayer.Username} logged in successfully in offline mode. " +
					   $"userid: {unverifiedUserid}", Category.Admin);
			return true;
		}

		//allow empty token for local offline testing
		if (string.IsNullOrEmpty(unverifiedToken) || string.IsNullOrEmpty(unverifiedUserid))
		{
			StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: Account has invalid cookie."));
			Logger.Log($"A user tried to connect with null userid or token value" +
					   $"Details: Username: {unverifiedConnPlayer.Username}, ClientID: {unverifiedClientId}, IP: {unverifiedConnPlayer.Connection.address}",
				Category.Admin);
			return false;
		}

		//check if they are already logged in, skip this check if offline mode is enable or if not a release build.
		if (BuildPreferences.isForRelease)
		{
			var otherUser = GetByUserID(unverifiedUserid);
			if (otherUser != null)
			{
				if (otherUser.Connection != null && otherUser.GameObject != null)
				{
					if (unverifiedConnPlayer.UserId == unverifiedUserid
						&& unverifiedConnPlayer.Connection != otherUser.Connection)
					{
						StartCoroutine(
							KickPlayer(unverifiedConnPlayer, $"Server Error: You are already logged into this server!"));
						Logger.Log($"A user tried to connect with another client while already logged in \r\n" +
								   $"Details: Username: {unverifiedConnPlayer.Username}, ClientID: {unverifiedClientId}, IP: {unverifiedConnPlayer.Connection.address}",
							Category.Admin);
						return false;
					}
				}
			}

			otherUser = GetByConnection(unverifiedConnPlayer.Connection);
			if (otherUser != null)
			{
				StartCoroutine(
					KickPlayer(unverifiedConnPlayer, $"Server Error: You already have an existing connection with the server!"));
				Logger.LogWarning($"Warning 2 simultaneous connections from same IP detected\r\n" +
						   $"Details: Unverified Username: {unverifiedConnPlayer.Username}, Unverified ClientID: {unverifiedClientId}, IP: {unverifiedConnPlayer.Connection.address}",
					Category.Admin);
			}
		}
		var refresh = new RefreshToken { userID = unverifiedUserid, refreshToken = unverifiedToken };//Assuming this validates it for now
		var response = await ServerData.ValidateToken(refresh, true);
		//fail, unless doing local offline testing
		if (!GameData.Instance.OfflineMode)
		{
			if (response == null)
			{
				StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: Server request error"));
				Logger.Log($"Server request error for " +
						   $"Details: Username: {unverifiedConnPlayer.Username}, ClientID: {unverifiedClientId}, IP: {unverifiedConnPlayer.Connection.address}",
					Category.Admin);
				return false;
			}
		}
		else
		{
			if (response == null) return false;
		}

		//allow error response for local offline testing
		if (response.errorCode == 1)
		{
			StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: Account has invalid cookie."));
			Logger.Log($"A spoof attempt was recorded. " +
					   $"Details: Username: {unverifiedConnPlayer.Username}, ClientID: {unverifiedClientId}, IP: {unverifiedConnPlayer.Connection.address}",
				Category.Admin);
			return false;
		}
		var Userid = unverifiedUserid;

		//Adds server to admin list if not already in it.
		if (Userid == ServerData.UserID && !adminUsers.Contains(Userid))
		{
			File.AppendAllLines(adminsPath, new string[]
			{
			"\r\n" + Userid
			});

			adminUsers.Add(Userid);
			var user = GetByUserID(Userid);

			if (user == null) return false;

			var newToken = Guid.NewGuid().ToString();
			if (!loggedInAdmins.ContainsKey(Userid))
			{
				loggedInAdmins.Add(Userid, newToken);
				AdminEnableMessage.SendMessage(user, newToken);
			}
		}

		//whitelist checking:
		var lines = File.ReadAllLines(whiteListPath);

		//Checks whether the userid is in either the Admins or whitelist AND that the whitelist file has something in it.
		//Whitelist only activates if whitelist is populated.

		if (lines.Length > 0 && !adminUsers.Contains(Userid) && !whiteListUsers.Contains(Userid))
		{
			StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: This account is not whitelisted."));

			Logger.Log($"{unverifiedConnPlayer.Username} tried to log in but the account is not whitelisted. " +
						   $"IP: {unverifiedConnPlayer.Connection.address}", Category.Admin);
			return false;
		}

		// banlist checking:
		var banEntry = banList?.CheckForEntry(Userid, unverifiedConnPlayer.Connection.address, unverifiedClientId);
		if (banEntry != null)
		{
			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan, "O", CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if (totalMins > (float)banEntry.minutes)
			{
				//Old ban, remove it
				banList.banEntries.Remove(banEntry);
				SaveBanList();
				Logger.Log($"{unverifiedConnPlayer.Username} ban has expired and the user has logged back in.", Category.Admin);
			}
			else
			{
				//User is still banned:
				StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: This account is banned. " +
													  $"You were banned for {banEntry.reason}. This ban has {banEntry.minutes - totalMins} minutes remaining."));
				Logger.Log($"{unverifiedConnPlayer.Username} tried to log back in but the account is banned. " +
						   $"IP: {unverifiedConnPlayer.Connection.address}", Category.Admin);
				return false;
			}
		}

		Logger.Log($"{unverifiedConnPlayer.Username} logged in successfully. " +
				   $"userid: {Userid}", Category.Admin);

		return true;
	}

	void SaveBanList()
	{
		File.WriteAllText(banPath, JsonUtility.ToJson(banList));
	}

	#region JobBans

	/// <summary>
	/// Checks job ban state, FALSE if banned
	/// </summary>
	public bool CheckJobBanState(string userID, JobType jobType)
	{
		//jobbanlist checking:
		var jobBanEntry = FindPlayerJobBanEntryServer(userID, jobType);

		if (jobBanEntry == null)
		{
			//No job ban so allowed
			return true;
		}

		return false;
	}

	public JobBanEntry FindPlayerJobBanEntryServer(string userID, JobType jobType, bool serverSideCheck = false)
	{
		var players = GetAllByUserID(userID);
		if (players.Count != 0)
		{
			foreach (var player in players)
			{
				var entry = FindPlayerJobBanEntry(player, jobType, serverSideCheck);

				if (entry != null)
				{
					return entry;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Find the players job ban entry if it exists.
	/// </summary>
	/// <param name="connPlayer"></param>
	/// <param name="jobType"></param>
	/// <param name="serverSideCheck">Used for after round start selecting.</param>
	/// <returns></returns>
	public JobBanEntry FindPlayerJobBanEntry(ConnectedPlayer connPlayer, JobType jobType, bool serverSideCheck)
	{
		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId);

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
			Logger.Log($"{connPlayer.Username} has bypassed the client side check for ban entry, possible hack attempt. " + $"IP: {connPlayer.Connection.address}", Category.Admin);
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
	public List<JobBanEntry> ClientAskingAboutJobBans(ConnectedPlayer connPlayer)
	{
		if (connPlayer.Equals(ConnectedPlayer.Invalid))
		{
			Logger.LogError($"Attempted to check job-ban for invalid player.", Category.Jobs);
			return default;
		}

		string playerUserID = connPlayer.UserId;
		string playerAddress = connPlayer.Connection.address;
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

			if (jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId).Item1.jobBanEntry.Count == 0) break;
		}

		var newJobBanPlayerEntry = jobBanList
			?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId).Item1.jobBanEntry;

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
		var players = GetAllByUserID(playerID);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				var list = ClientAskingAboutJobBans(p);

				if (list != null)
				{
					return list;
				}
			}
		}

		return null;
	}

	private void JobBanExpireCheck(JobBanEntry jobBanEntry, int index, ConnectedPlayer connPlayer)
	{
		//Old ban, remove it
		jobBanList.jobBanEntries[index].jobBanEntry.Remove(jobBanEntry);
		SaveJobBanList();
		Logger.Log($"{connPlayer.Username} job ban for {jobBanEntry.job} has expired.", Category.Admin);
	}

	void SaveJobBanList()
	{
		File.WriteAllText(jobBanPath, JsonUtility.ToJson(jobBanList));
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
			if (IsFromAdmin() == false) return;

			// Server Stuff here

			Instance.ProcessJobBanRequest(
					SentByPlayer.UserId, msg.PlayerID, msg.Reason,
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

	public void CheckAdminState(ConnectedPlayer playerConn, string userid)
	{
		//full admin privs for local offline testing for host player
		if (adminUsers.Contains(userid) || (GameData.Instance.OfflineMode && playerConn.GameObject == PlayerManager.LocalViewerScript.gameObject))
		{
			//This is an admin, send admin notify to the users client
			Logger.Log($"{playerConn.Username} logged in as Admin. IP: {playerConn.Connection.address}", Category.Admin);
			var newToken = System.Guid.NewGuid().ToString();
			if (!loggedInAdmins.ContainsKey(userid))
			{
				loggedInAdmins.Add(userid, newToken);
				AdminEnableMessage.SendMessage(playerConn, newToken);
			}
		}
	}

	public void CheckMentorState(ConnectedPlayer playerConn, string userid)
	{
		if (mentorUsers.Contains(userid) && !adminUsers.Contains(userid))
		{
			Logger.Log($"{playerConn.Username} logged in as Mentor. IP: {playerConn.Connection.address}", Category.Admin);
			var newToken = System.Guid.NewGuid().ToString();
			if (!loggedInMentors.ContainsKey(userid))
			{
				loggedInMentors.Add(userid, newToken);
				MentorEnableMessage.Send(playerConn.Connection, newToken);
			}
		}
	}

	void CheckForLoggedOffAdmin(string userid, string userName)
	{
		if (loggedInAdmins.ContainsKey(userid))
		{
			Logger.Log($"Admin {userName} logged off.", Category.Admin);
			loggedInAdmins.Remove(userid);
		}
	}

	public void SetClientAsAdmin(string _adminToken)
	{
		AdminToken = _adminToken;
		IsClientAdmin = true;
		ControlTabs.Instance.ToggleOnAdminTab();
		Logger.Log("You have logged in as an admin. Admin tools are now available.", Category.Admin);
	}

	public void SetClientAsMentor(string _mentorToken)
	{
		MentorToken = _mentorToken;
		Logger.Log("You have logged in as a mentor. Mentor tools are now available.", Category.Admin);
	}

	public void ProcessAdminEnableRequest(string admin, string userToPromote)
	{
		if (!adminUsers.Contains(admin)) return;
		if (adminUsers.Contains(userToPromote)) return;

		Logger.Log(
			$"{admin} has promoted {userToPromote} to admin. Time: {DateTime.Now}", Category.Admin);

		File.AppendAllLines(adminsPath, new string[]
		{
			"\r\n" + userToPromote
		});

		adminUsers.Add(userToPromote);
		var user = GetByUserID(userToPromote);

		if (user == null) return;

		var newToken = System.Guid.NewGuid().ToString();
		if (!loggedInAdmins.ContainsKey(userToPromote))
		{
			loggedInAdmins.Add(userToPromote, newToken);
			AdminEnableMessage.SendMessage(user, newToken);
		}
	}
	#endregion

	#region Kick/Ban

	public void ProcessKickRequest(string adminId, string userToKick, string reason, bool isBan, int banMinutes, bool announceBan)
	{
		if (!adminUsers.Contains(adminId)) return;

		ConnectedPlayer adminPlayer = PlayerList.Instance.GetByUserID(adminId);
		List<ConnectedPlayer> players = GetAllByUserID(userToKick, true);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				string message = $"A kick/ban has been processed by {adminPlayer.Username}: Username: {p.Username} Player: {p.Name} IsBan: {isBan} BanMinutes: {banMinutes} Time: {DateTime.Now}";

				Logger.Log(message, Category.Admin);

				StartCoroutine(KickPlayer(p, reason, isBan, banMinutes,adminPlayer));

				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message + $"\nReason: {reason}", "");

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(message, null);

				if (!announceBan || !ServerData.ServerConfig.DiscordWebhookEnableBanKickAnnouncement) return;

				if (isBan)
				{
					message = $"{ServerData.ServerConfig.ServerName}\nPlayer: {p.Username}, has been banned for {banMinutes} minutes.";
				}
				else
				{
					message = $"{ServerData.ServerConfig.ServerName}\nPlayer: {p.Username}, has been kicked.";
				}

				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");
			}
		}
		else
		{
			Logger.Log($"Kick ban failed, can't find player: {userToKick}. Requested by {adminPlayer.Username}", Category.Admin);
		}
	}

	public void ServerKickPlayer(string userToKick, string reason, bool isBan, int banMinutes, bool announceBan)
	{
		List<ConnectedPlayer> players = GetAllByUserID(userToKick, true);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				string message = $"A kick/ban has been processed by the Server: Username: {p.Username} Player: {p.Name} IsBan: {isBan} BanMinutes: {banMinutes} Time: {DateTime.Now}";

				Logger.Log(message, Category.Admin);

				StartCoroutine(KickPlayer(p, reason, isBan, banMinutes));

				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message + $"\nReason: {reason}", "");

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(message, null);

				if (!announceBan || !ServerData.ServerConfig.DiscordWebhookEnableBanKickAnnouncement) return;

				if (isBan)
				{
					message = $"{ServerData.ServerConfig.ServerName}\nPlayer: {p.Username}, has been banned for {banMinutes} minutes.";
				}
				else
				{
					message = $"{ServerData.ServerConfig.ServerName}\nPlayer: {p.Username}, has been kicked.";
				}

				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");
			}
		}
		else
		{
			Logger.Log($"Server Kick/ban failed, can't find player: {userToKick}", Category.Admin);
		}
	}

	IEnumerator KickPlayer(ConnectedPlayer connPlayer, string reason,
		bool ban = false, int banLengthInMinutes = 0, ConnectedPlayer adminPlayer = null)
	{
		Logger.Log("Processing KickPlayer/ban for " + "\n"
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
				Logger.Log("removing pre-existing ban entry for userId of" + connPlayer.UserId, Category.Admin);
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
				Logger.Log(banList.banEntries[banList.banEntries.Count - 1].ToString(), Category.Admin);
			}
		}
		else
		{
			message = $"You have been kicked. Reason: {reason}";
		}

		SendClientLogMessage.SendLogToClient(connPlayer.GameObject, message, Category.Admin, true);
		yield return WaitFor.Seconds(0.1f);

		if (connPlayer.Connection == null)
		{
			Logger.Log($"Not kicking, already disconnected: {connPlayer.Name}", Category.Admin);
			yield break;
		}

		Logger.Log($"Kicking client {connPlayer.Username} : {message}", Category.Admin);
		InfoWindowMessage.Send(connPlayer.GameObject, message, "Disconnected");

		yield return WaitFor.Seconds(1f);

		connPlayer.Connection.Disconnect();

		while (!loggedOff.Contains(connPlayer))
		{
			yield return WaitFor.EndOfFrame;
		}

		loggedOff.Remove(connPlayer);
	}

	public void ProcessJobBanRequest(string adminId, string userToJobBan, string reason, bool isPerma, int banMinutes, JobType jobType, bool kickAfter = false, bool ghostAfter = false)
	{
		if (!adminUsers.Contains(adminId)) return;

		ConnectedPlayer adminPlayer = PlayerList.Instance.GetByUserID(adminId);
		List<ConnectedPlayer> players = GetAllByUserID(userToJobBan, true);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				string message = "";

				if (isPerma)
				{
					message = $"A job ban has been processed by {adminPlayer.Username}: Username: {p.Username} Player: {p.Name} Job: {jobType} IsPerma: {isPerma} Time: {DateTime.Now}";
				}
				else
				{
					message = $"A job ban has been processed by {adminPlayer.Username}: Username: {p.Username} Player: {p.Name} Job: {jobType} BanMinutes: {banMinutes} Time: {DateTime.Now}";
				}

				Logger.Log(message, Category.Admin);

				StartCoroutine(JobBanPlayer(p, reason, isPerma, banMinutes, jobType, adminPlayer));

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord($"{adminPlayer.Username}: job banned {p.Username} from {jobType}, IsPerma: {isPerma}, BanMinutes: {banMinutes}", null);

				DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, message + $"\nReason: {reason}", "");

				if (ghostAfter)
				{
					if (!p.Script.IsGhost)
					{
						PlayerSpawn.ServerSpawnGhost(p.Script.mind);
						p.Script.mind.ghostLocked = true;
					}
				}

				if (kickAfter)
				{
					reason = "Player was kicked after job ban process.";
					StartCoroutine(KickPlayer(p, reason, false));
					continue;
				}

				//Send update if they are still in the game
				if(p.Connection == null) continue;
				ServerSendsJobBanDataMessage.Send(p.Connection, ClientAskingAboutJobBans(p));
			}
		}
		else
		{
			Logger.Log($"job ban failed, can't find player: {userToJobBan}. Requested by {adminPlayer.Username}", Category.Admin);
		}
	}

	IEnumerator JobBanPlayer(ConnectedPlayer connPlayer, string reason, bool isPermaBool, int banLengthInMinutes, JobType jobType, ConnectedPlayer admin)
	{
		if (jobBanList == null)
		{
			Logger.LogError("The job ban list loaded from the json was null, cant add new ban to it.", Category.Admin);
			yield break;
		}

		Logger.Log("Processing job ban for " + "\n"
													+ "UserId " + connPlayer?.UserId + "\n"
													+ "Username " + connPlayer?.Username + "\n"
													+ "address " + connPlayer?.Connection?.address + "\n"
													+ "clientId " + connPlayer?.ClientId + "\n"
													+ "jobType " + jobType + "\n", Category.Admin
		);

		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId);

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			//Doesnt have a job ban yet

			jobBanList.jobBanEntries.Add(new JobBanPlayerEntry
			{
				userId = connPlayer?.UserId,
				userName = connPlayer?.Username,
				ipAddress = connPlayer?.Connection?.address,
				clientId = connPlayer?.ClientId,
				jobBanEntry = new List<JobBanEntry>(),
				adminId = admin?.UserId,
				adminName = admin?.Username
			});

			jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId);
		}

		if (jobBanPlayerEntry.Value.Item1 == null)
		{
			Logger.LogError("New job ban list was null even though new one was generated", Category.Admin);
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
