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
	public string AdminToken { get; private set; }

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");
		banPath = Path.Combine(Application.streamingAssetsPath, "admin", "banlist.json");
		whiteListPath = Path.Combine(Application.streamingAssetsPath, "admin", "whitelist.txt");

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
				                                      $"You were banned for {banEntry.reason}. This ban has {banEntry.minutes} minutes remaining."));
				Logger.Log($"{unverifiedConnPlayer.Username} tried to log back in but the account is banned. " +
				           $"IP: {unverifiedConnPlayer.Connection.address}", Category.Admin);
				return false;
			}
		}

		Logger.Log($"{unverifiedConnPlayer.Username} logged in successfully. " +
		           $"userid: {Userid}", Category.Admin);

		return true;
	}

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

	void SaveBanList()
	{
		File.WriteAllText(banPath, JsonUtility.ToJson(banList));
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

	public void ProcessKickRequest(string admin, string userToKick, string reason, bool isBan, int banMinutes)
	{
		if (!adminUsers.Contains(admin)) return;

		var players = GetAllByUserID(userToKick);
		if (players.Count != 0)
		{
			foreach (var p in players)
			{
				Logger.Log(
					$"A kick/ban has been processed by {admin}: Player: {p.Name} IsBan: {isBan} BanMinutes: {banMinutes} Time: {DateTime.Now}");
				StartCoroutine(KickPlayer(p, reason, isBan, banMinutes));
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
		string message = "";
		if (ban)
		{
			message = $"You have been banned for {banLengthInMinutes}" +
			          $" minutes. Reason: {reason}";

			var index = banList.banEntries.FindIndex(x => x.userId == connPlayer.UserId);
			if (index != -1)
			{
				banList.banEntries.RemoveAt(index);
			}

			banList.banEntries.Add(new BanEntry
			{
				userId = connPlayer.UserId,
				userName = connPlayer.Username,
				minutes = banLengthInMinutes,
				reason = reason,
				dateTimeOfBan = DateTime.Now.ToString("O"),
				ipAddress = connPlayer.Connection.address,
				clientId = connPlayer.ClientId
			});

			SaveBanList();
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
}