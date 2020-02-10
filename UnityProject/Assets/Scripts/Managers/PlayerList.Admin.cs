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
	private List<string> adminUsers = new List<string>();
	private Dictionary<string, string> loggedInAdmins = new Dictionary<string, string>();
	private BanList banList;
	private string adminsPath;
	private string banPath;
	private bool thisClientIsAdmin;
	public string AdminToken { get; private set; }

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");
		banPath = Path.Combine(Application.streamingAssetsPath, "admin", "banlist.json");

		if (!File.Exists(adminsPath))
		{
			File.CreateText(adminsPath).Close();
		}

		if (!File.Exists(banPath))
		{
			File.WriteAllText(banPath, JsonUtility.ToJson(new BanList()));
		}

		adminListWatcher = new FileSystemWatcher();
		adminListWatcher.Path = Path.GetDirectoryName(adminsPath);
		adminListWatcher.Filter = Path.GetFileName(adminsPath);
		adminListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		adminListWatcher.Changed += LoadCurrentAdmins;
		adminListWatcher.EnableRaisingEvents = true;
		LoadBanList();
		LoadCurrentAdmins();
	}

	void LoadBanList()
	{
		StartCoroutine(LoadBans());
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
	public bool IsAdmin(string userID)
	{
		return adminUsers.Contains(userID);
	}

	public async Task<bool> ValidatePlayer(string clientID, string username,
		string userid, int clientVersion, ConnectedPlayer playerConn,
		string token)
	{
		var validAccount = await CheckUserState(userid, token, playerConn, clientID);

		if (!validAccount)
		{
			return false;
		}

		if (clientVersion != GameData.BuildNumber)
		{
			StartCoroutine(KickPlayer(playerConn, $"Invalid Client Version! You need version {GameData.BuildNumber}." +
			                                      " This can be acquired through the station hub."));
			return false;
		}

		return true;
	}

	//Check if tokens match and if the player is an admin or is banned
	private async Task<bool> CheckUserState(string userid, string token, ConnectedPlayer playerConn, string clientID)
	{
		//allow empty token for local offline testing
		if ((string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userid)) && !GameData.Instance.OfflineMode)
		{
			StartCoroutine(KickPlayer(playerConn, $"Server Error: Account has invalid cookie."));
			Logger.Log($"A user tried to connect with null userid or token value" +
			           $"Details: Username: {playerConn.Username}, ClientID: {clientID}, IP: {playerConn.Connection.address}",
				Category.Admin);
			return false;
		}

		//check if they are already logged in, skip this check if offline mode is enable or if not a release build.
		if (!GameData.Instance.OfflineMode && BuildPreferences.isForRelease)
		{
			var otherUser = GetByUserID(userid);
			if (otherUser != null)
			{
				if (otherUser.Connection != null && otherUser.GameObject != null)
				{
					if (playerConn.UserId == userid
					    && playerConn.Connection != otherUser.Connection)
					{
						StartCoroutine(
							KickPlayer(playerConn, $"Server Error: You are already logged into this server!"));
						Logger.Log($"A user tried to connect with another client while already logged in \r\n" +
						           $"Details: Username: {playerConn.Username}, ClientID: {clientID}, IP: {playerConn.Connection.address}",
							Category.Admin);
						return false;
					}
				}
			}
		}

		var refresh = new RefreshToken {userID = userid, refreshToken = token};
		var response = await ServerData.ValidateToken(refresh, true);

		//fail, unless doing local offline testing
		if (response == null && !GameData.Instance.OfflineMode)
		{
			return false;
		}

		//allow error response for local offline testing
		if (response != null && response.errorCode == 1 && !GameData.Instance.OfflineMode)
		{
			StartCoroutine(KickPlayer(playerConn, $"Server Error: Account has invalid cookie."));
			Logger.Log($"A spoof attempt was recorded. " +
			           $"Details: Username: {playerConn.Username}, ClientID: {clientID}, IP: {playerConn.Connection.address}",
				Category.Admin);
			return false;
		}


		var banEntry = banList.CheckForEntry(userid, playerConn.Connection.address);
		if (banEntry != null)
		{
			var entryTime = DateTime.ParseExact(banEntry.dateTimeOfBan,"O",CultureInfo.InvariantCulture);
			var totalMins = Mathf.Abs((float)(entryTime - DateTime.Now).TotalMinutes);
			if ( totalMins > (float)banEntry.minutes)
			{
				//Old ban, remove it
				banList.banEntries.Remove(banEntry);
				SaveBanList();
				Logger.Log($"{playerConn.Username} ban has expired and the user has logged back in.", Category.Admin);
			}
			else
			{
				//User is still banned:
				StartCoroutine(KickPlayer(playerConn, $"Server Error: This account is banned. " +
				                                      $"Check your initial ban message for expiry time"));
				Logger.Log($"{playerConn.Username} tried to log back in but the account is banned. " +
				           $"IP: {playerConn.Connection.address}", Category.Admin);
				return false;
			}
		}

		Logger.Log($"{playerConn.Username} logged in successfully. " +
		           $"userid: {userid}", Category.Admin);

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
				AdminEnableMessage.Send(playerConn.GameObject, newToken);
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
		thisClientIsAdmin = true;
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
			AdminEnableMessage.Send(user.GameObject, newToken);
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
		string clientID = connPlayer.ClientId;
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
				ipAddress = connPlayer.Connection.address
			});

			File.WriteAllText(banPath, JsonUtility.ToJson(banList));
		}
		else
		{
			message = $"You have been kicked. Reason: {reason}";
		}

		SendClientLogMessage.SendLogToClient(connPlayer.GameObject, message, Category.Admin, true);
		yield return WaitFor.Seconds(0.1f);

		if (!connPlayer.Connection.isConnected)
		{
			Logger.Log($"Not kicking, already disconnected: {connPlayer.Name}");
			yield break;
		}

		Logger.Log($"Kicking client {clientID} : {message}");
		InfoWindowMessage.Send(connPlayer.GameObject, message, "Disconnected");


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

	public BanEntry CheckForEntry(string userId, string ipAddress)
	{
		var index = banEntries.FindIndex(x => x.userId == userId);
		if (index == -1)
		{
			var ipIndex = banEntries.FindIndex(x => x.ipAddress == ipAddress);
			if (ipIndex != -1)
			{
				return banEntries[ipIndex];
			}

			return null;
		}

		return banEntries[index];
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
}