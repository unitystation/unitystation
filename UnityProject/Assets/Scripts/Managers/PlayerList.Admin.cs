using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DatabaseAPI;
using Mirror;
using UnityEngine;
using UnityEngine.Diagnostics;
using DiscordWebhook;
using Newtonsoft.Json;

/// <summary>
/// Admin Controller for players
/// </summary>
public partial class PlayerList
{
	private FileSystemWatcher adminListWatcher;
	private FileSystemWatcher WhiteListWatcher;
	private List<string> adminUsers = new List<string>();
	private Dictionary<string, string> loggedInAdmins = new Dictionary<string, string>();
	private BanList banList;
	private string adminsPath;
	private string banPath;
	private List<string> whiteListUsers = new List<string>();
	private string whiteListPath;

	private string jobBanPath;
	private JobBanList jobBanList;

	public List<JobBanEntry> clientSideBanEntries = new List<JobBanEntry>();

	public string AdminToken { get; private set; }

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");
		banPath = Path.Combine(Application.streamingAssetsPath, "admin", "banlist.json");
		whiteListPath = Path.Combine(Application.streamingAssetsPath, "admin", "whitelist.txt");
		jobBanPath = Path.Combine(Application.streamingAssetsPath, "admin", "jobBanlist.json");

		if (!File.Exists(adminsPath))
		{
			File.CreateText(adminsPath).Close();
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

		WhiteListWatcher = new FileSystemWatcher();
		WhiteListWatcher.Path = Path.GetDirectoryName(whiteListPath);
		WhiteListWatcher.Filter = Path.GetFileName(whiteListPath);
		WhiteListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		WhiteListWatcher.Changed += LoadWhiteList;
		WhiteListWatcher.EnableRaisingEvents = true;

		LoadBanList();
		LoadCurrentAdmins();
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
	public bool IsAdmin(string userID)
	{
		return adminUsers.Contains(userID);
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
		var refresh = new RefreshToken {userID = unverifiedUserid, refreshToken = unverifiedToken};//Assuming this validates it for now
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
		var Token = unverifiedToken;
		//whitelist checking:
		var lines = File.ReadAllLines(whiteListPath);

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

			var newToken = System.Guid.NewGuid().ToString();
			if (!loggedInAdmins.ContainsKey(Userid))
			{
				loggedInAdmins.Add(Userid, newToken);
				AdminEnableMessage.Send(user.Connection, newToken);
			}
		}

		//Checks whether the userid is in either the Admins or whitelist AND that the whitelist file has something in it.
		//Whitelist only activates if whitelist is populated.

		if (lines.Length > 0 && !adminUsers.Contains(Userid) && !whiteListUsers.Contains(Userid) )
		{
			StartCoroutine(KickPlayer(unverifiedConnPlayer, $"Server Error: This account is not whitelisted."));

			Logger.Log($"{unverifiedConnPlayer.Username} tried to log in but the account is not whitelisted. " +
						   $"IP: {unverifiedConnPlayer.Connection.address}", Category.Admin);
			return false;
		}


		//banlist checking:
		var banEntry = banList?.CheckForEntry(Userid, unverifiedConnPlayer.Connection.address, unverifiedClientId);
		if (banEntry != null)
		{
			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if ( totalMins > (float)banEntry.minutes)
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
					break;
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

		if(jobBanEntry == null)
		{
			//Specific Job isnt banned
			return null;
		}

		if (jobBanEntry.isPerma)
		{
			//Specific job has been perma banned
			return jobBanEntry;
		}

		var entryTime = DateTime.ParseExact(jobBanEntry.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
		var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
		if ( totalMins > (float)jobBanEntry.minutes)
		{
			JobBanExpireCheck(jobBanEntry, jobBanPlayerEntry.Value.Item2, connPlayer);
		}
		else
		{
			if(!serverSideCheck) return jobBanEntry;

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
	private List<JobBanEntry> ClientAskingAboutJobBans(ConnectedPlayer connPlayer)
	{
		//jobbanlist checking:
		var jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId);

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
		foreach (var jobBan in jobBanPlayerEntry.Value.Item1.jobBanEntry)
		{
			var entryTime = DateTime.ParseExact(jobBan.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if (totalMins > (float)jobBan.minutes && !jobBan.isPerma)
			{
				JobBanExpireCheck(jobBan, jobBanPlayerEntry.Value.Item2, connPlayer);
			}

			if(jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId).Item1.jobBanEntry.Count == 0) break;
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
	public bool ClientCheck(JobType job)
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

			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

			if (totalMins < (float) banEntry.minutes)
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

			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);

			if (totalMins < (float) banEntry.minutes)
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

	public class ClientJobBanDataMessage : ClientMessage
	{
		public string PlayerID;

		public override void Process()
		{
			//Server Stuff here

			var conn = PlayerList.Instance.GetByUserID(PlayerID);

			if (conn == null)
			{
				Debug.LogError("Connection was NULL");
				return;
			}

			var jobBanEntries = PlayerList.Instance.ClientAskingAboutJobBans(conn);

			ServerSendsJobBanDataMessage.Send(conn.Connection, jobBanEntries);
		}

		public static ClientJobBanDataMessage Send(string playerID)
		{
			ClientJobBanDataMessage msg = new ClientJobBanDataMessage
			{
				PlayerID = playerID
			};
			msg.Send();
			return msg;
		}
	}

	public class ServerSendsJobBanDataMessage : ServerMessage
	{
		public string JobBanEntries;

		public override void Process()
		{
			//client Stuff here

			PlayerList.Instance.clientSideBanEntries = JsonConvert.DeserializeObject<List<JobBanEntry>>(JobBanEntries);
		}

		public static ServerSendsJobBanDataMessage Send(NetworkConnection requestee, List<JobBanEntry> jobBanEntries)
		{
			ServerSendsJobBanDataMessage msg = new ServerSendsJobBanDataMessage
			{
				JobBanEntries = JsonConvert.SerializeObject(jobBanEntries)
			};
			msg.SendTo(requestee);
			return msg;
		}
	}

	public class RequestJobBan : ClientMessage
	{
		public string AdminID;
		public string AdminToken;
		public string PlayerID;
		public string Reason;
		public bool IsPerma;
		public int Minutes;
		public JobType JobType;
		public bool KickAfter;
		public bool GhostAfter;

		public override void Process()
		{
			var admin = PlayerList.Instance.GetAdmin(AdminID, AdminToken);
			if (admin == null) return;

			//Server Stuff here

			PlayerList.Instance.ProcessJobBanRequest(AdminID, PlayerID, Reason, IsPerma, Minutes, JobType, KickAfter, GhostAfter);
		}

		public static RequestJobBan Send(string adminID, string adminToken, string playerID, string reason, bool isPerma, int minutes, JobType jobType, bool kickAfter, bool ghostAfter)
		{
			RequestJobBan msg = new RequestJobBan
			{
				AdminID = adminID,
				AdminToken = adminToken,
				PlayerID = playerID,
				Reason = reason,
				IsPerma = isPerma,
				Minutes = minutes,
				JobType = jobType,
				KickAfter = kickAfter,
				GhostAfter = ghostAfter
			};
			msg.Send();
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
			Logger.Log($"{playerConn.Username} logged in as Admin. " +
			           $"IP: {playerConn.Connection.address}");
			var newToken = System.Guid.NewGuid().ToString();
			if (!loggedInAdmins.ContainsKey(userid))
			{
				loggedInAdmins.Add(userid, newToken);
				AdminEnableMessage.Send(playerConn.Connection, newToken);
			}
		}
	}

	void CheckForLoggedOffAdmin(string userid, string userName)
	{
		if (loggedInAdmins.ContainsKey(userid))
		{
			Logger.Log($"Admin {userName} logged off.");
			loggedInAdmins.Remove(userid);
		}
	}

	public void SetClientAsAdmin(string _adminToken)
	{
		AdminToken = _adminToken;
		ControlTabs.Instance.ToggleOnAdminTab();
		Logger.Log("You have logged in as an admin. Admin tools are now available.");
	}

	public void ProcessAdminEnableRequest(string admin, string userToPromote)
	{
		if (!adminUsers.Contains(admin)) return;
		if (adminUsers.Contains(userToPromote)) return;

		Logger.Log(
			$"{admin} has promoted {userToPromote} to admin. Time: {DateTime.Now}");

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
			AdminEnableMessage.Send(user.Connection, newToken);
		}
	}
	#endregion

	#region Kick/Ban

	public void ProcessKickRequest(string admin, string userToKick, string reason, bool isBan, int banMinutes, bool announceBan)
	{
		if (!adminUsers.Contains(admin)) return;

		var players = GetAllByUserID(userToKick);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				var message = $"A kick/ban has been processed by {PlayerList.Instance.GetByUserID(admin).Username}: Username: {p.Username} Player: {p.Name} IsBan: {isBan} BanMinutes: {banMinutes} Time: {DateTime.Now}";

				Logger.Log(message);

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
			Logger.Log($"Kick ban failed, can't find player: {userToKick}. Requested by {admin}");
		}
	}

	IEnumerator KickPlayer(ConnectedPlayer connPlayer, string reason,
		bool ban = false, int banLengthInMinutes = 0)
	{
		Logger.Log( "Processing KickPlayer/ban for "+ "\n"
		           + "UserId " + connPlayer?.UserId + "\n"
		           + "Username " + connPlayer?.Username + "\n"
		           + "address " + connPlayer?.Connection?.address + "\n"
		           + "clientId " + connPlayer?.ClientId + "\n"
		           );

		string message = "";
		if (ban)
		{
			message = $"You have been banned for {banLengthInMinutes}" +
			          $" minutes. Reason: {reason}";

			var index = banList.banEntries.FindIndex(x => x.userId == connPlayer.UserId);
			if (index != -1)
			{
				Logger.Log("removing pre-existing ban entry for userId of" + connPlayer.UserId  );
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
				clientId = connPlayer?.ClientId
			});

			SaveBanList();
			if (banList.banEntries.Count != 0)
			{
				Logger.Log(banList.banEntries[banList.banEntries.Count - 1].ToString());
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
			Logger.Log($"Not kicking, already disconnected: {connPlayer.Name}");
			yield break;
		}

		Logger.Log($"Kicking client {connPlayer.Username} : {message}");
		InfoWindowMessage.Send(connPlayer.GameObject, message, "Disconnected");

		yield return WaitFor.Seconds(1f);

		connPlayer.Connection.Disconnect();
		connPlayer.Connection.Dispose();

		while (!loggedOff.Contains(connPlayer))
		{
			yield return WaitFor.EndOfFrame;
		}

		loggedOff.Remove(connPlayer);
	}

	public void ProcessJobBanRequest(string admin, string userToJobBan, string reason, bool isPerma, int banMinutes, JobType jobType, bool kickAfter = false, bool ghostAfter = false)
	{
		if (!adminUsers.Contains(admin)) return;

		var players = GetAllByUserID(userToJobBan);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				var message = "";

				if (isPerma)
				{
					message = $"A job ban has been processed by {PlayerList.Instance.GetByUserID(admin).Username}: Username: {p.Username} Player: {p.Name} Job: {jobType} IsPerma: {isPerma} Time: {DateTime.Now}";
				}
				else
				{
					message = $"A job ban has been processed by {PlayerList.Instance.GetByUserID(admin).Username}: Username: {p.Username} Player: {p.Name} Job: {jobType} BanMinutes: {banMinutes} Time: {DateTime.Now}";
				}

				Logger.Log(message);

				StartCoroutine(JobBanPlayer(p, reason, isPerma, banMinutes, jobType));

				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord($"{PlayerList.Instance.GetByUserID(admin).Username}: job banned {p.Username} from {jobType}, IsPerma: {isPerma}, BanMinutes: {banMinutes}", null);

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
				}
			}
		}
		else
		{
			Logger.Log($"job ban failed, can't find player: {userToJobBan}. Requested by {admin}");
		}
	}

	IEnumerator JobBanPlayer(ConnectedPlayer connPlayer, string reason, bool isPermaBool, int banLengthInMinutes, JobType jobType)
	{
		if (jobBanList == null)
		{
			Debug.LogError("The job ban list loaded from the json was null, cant add new ban to it.");
			yield break;
		}

		Logger.Log( "Processing job ban for "+ "\n"
		                                            + "UserId " + connPlayer?.UserId + "\n"
		                                            + "Username " + connPlayer?.Username + "\n"
		                                            + "address " + connPlayer?.Connection?.address + "\n"
		                                            + "clientId " + connPlayer?.ClientId + "\n"
		                                            + "jobType " + jobType + "\n"
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
				jobBanEntry = new List<JobBanEntry>()
			});

			jobBanPlayerEntry = jobBanList?.CheckForEntry(connPlayer.UserId, connPlayer.Connection.address, connPlayer.ClientId);
		}

		if (jobBanPlayerEntry.Value.Item1  == null)
		{
			Debug.LogError("New job ban list was null even though new one was generated");
			yield break;
		}

		var jobBanEntry = jobBanPlayerEntry.Value.Item1.CheckForSpecificJob(jobType);

		if(jobBanEntry == null)
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

	public override string ToString()
	{
		return ("BanEntry of " + userId + " / " + userName + "\n"
		         + "for " + minutes + " minutes, on" + dateTimeOfBan + "\n"
		         + "on IP " + ipAddress + "\n"
		         + "clientId " + clientId + "\n"
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