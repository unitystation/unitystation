using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
	private string adminToken;

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");
		banPath = Path.Combine(Application.streamingAssetsPath, "admin", "banlist.json");

		if (!File.Exists(adminsPath))
		{
			File.CreateText(adminsPath);
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
			StartCoroutine(KickPlayer(playerConn, $"Invalid Client Version! You need version {GameData.BuildNumber}" +
			                          " This can be acquired through the station launcher or from the github of the code base" +
			                          " ( this step requires unity to compile, Ask appropriate maintainers of Code base on how to do this )"));
			return false;
		}

		return true;
	}

	//Check if tokens match and if the player is an admin or is banned
	private async Task<bool> CheckUserState(string userid, string token, ConnectedPlayer playerConn, string clientID)
	{
		//Only do the account check on release builds as its not important when developing
		if (BuildPreferences.isForRelease)
		{
			if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(userid))
			{
				StartCoroutine(KickPlayer(playerConn, $"Server Error: Account has invalid cookie."));
				Logger.Log($"A user tried to connect with null userid or token value" +
				           $"Details: Username: {playerConn.Username}, ClientID: {clientID}, IP: {playerConn.Connection.address}",
					Category.Admin);
				return false;
			}

			var refresh = new RefreshToken {userID = userid, refreshToken = token};
			var response = await ServerData.ValidateToken(refresh, true);

			if (response.errorCode == 1)
			{
				StartCoroutine(KickPlayer(playerConn, $"Server Error: Account has invalid cookie."));
				Logger.Log($"A spoof attempt was recorded. " +
				           $"Details: Username: {playerConn.Username}, ClientID: {clientID}, IP: {playerConn.Connection.address}",
					Category.Admin);
				return false;
			}
		}

		var banEntry = banList.CheckForEntry(userid);
		if (banEntry != null)
		{
			DateTime entryTime;
			DateTime.TryParse(banEntry.dateTimeOfBan, out entryTime);
			if (entryTime.AddMinutes(banEntry.minutes) < DateTime.Now)
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

		if (adminUsers.Contains(userid))
		{
			//This is an admin, send admin notify to the users client
			Logger.Log($"{playerConn.Username} logged in as Admin. " +
			           $"IP: {playerConn.Connection.address}", Category.Admin);
			var newToken = System.Guid.NewGuid().ToString();
			loggedInAdmins.Add(userid, newToken);
			AdminEnableMessage.Send(playerConn.GameObject, newToken);
		}

		Logger.Log($"{playerConn.Username} logged in successfully. " +
		           $"userid: {userid}", Category.Admin);

		return true;
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
		thisClientIsAdmin = true;
		adminToken = _adminToken;
		Logger.Log("You have logged in as an admin. Admin tools are now available.", Category.Admin);
	}

	void SaveBanList()
	{
		File.WriteAllText(banPath, JsonUtility.ToJson(banList));
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
		}
		else
		{
			message = $"You have been kicked. Reason: {reason}";
		}

		SendClientLogMessage.SendLogToClient(connPlayer.GameObject, message, Category.Connections, true);
		yield return WaitFor.Seconds(0.1f);

		if (!connPlayer.Connection.isConnected)
		{
			Logger.Log($"Not kicking, already disconnected: {connPlayer.Name}", Category.Connections);
			yield break;
		}

		Logger.Log($"Kicking client {clientID} : {message}", Category.Connections);
		InfoWindowMessage.Send(connPlayer.GameObject, message, "Disconnected");
		//Chat.AddGameWideSystemMsgToChat($"Player '{player.Name}' got kicked: {raisins}");

		while (!loggedOff.Contains(connPlayer))
		{
			yield return WaitFor.Seconds(1);
		}

		connPlayer.Connection.Disconnect();
		connPlayer.Connection.Dispose();

		loggedOff.Remove(connPlayer);
	}
}

[Serializable]
public class BanList
{
	public List<BanEntry> banEntries = new List<BanEntry>();

	public BanEntry CheckForEntry(string userId)
	{
		var index = banEntries.FindIndex(x => x.userId == userId);
		if (index == -1) return null;

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
}