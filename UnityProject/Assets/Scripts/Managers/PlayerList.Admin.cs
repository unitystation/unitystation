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
	private BanList banList;
	private string adminsPath;
	private string banPath;

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
		banList = JsonUtility.FromJson<BanList>(File.ReadAllText(banPath));
	}

	void LoadCurrentAdmins(object source, FileSystemEventArgs e)
	{
		LoadCurrentAdmins();
	}

	void LoadCurrentAdmins()
	{
		adminUsers.Clear();
		adminUsers = new List<string>(File.ReadAllLines(adminsPath));
	}

	public async Task<bool> ValidatePlayer(string clientID, string username,
		string userid, int clientVersion, ConnectedPlayer playerConn,
		string token)
	{
		var validAccount = await CheckUserState(userid, token, playerConn);

		if (!validAccount)
		{
			return false;
		}

		if (clientVersion != GameData.BuildNumber)
		{
			StartCoroutine(KickPlayer(playerConn, $"Invalid Client Version! You need version {GameData.BuildNumber}"));
			return false;
		}

		return true;
	}

	//Check if tokens match and if the player is an admin or is banned
	private async Task<bool> CheckUserState(string userid, string token, ConnectedPlayer playerConn)
	{
		//Only do the account check on release builds as its not important when developing
		if (BuildPreferences.isForRelease)
		{
			var refresh = new RefreshToken {userID = userid, refreshToken = token};
			var response = await ServerData.ValidateToken(refresh);

			if (response.errorCode == 1)
			{
				StartCoroutine(KickPlayer(playerConn, $"Server Error: Account has invalid cookie."));
				return false;
			}
		}



		return true;
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
			message = $"You have kicked. Reason: {reason}";
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
}

[Serializable]
public class BanEntry
{
	public string userId;
	public string userName;
	public long minutes;
	public string dateTimeOfBan;
	public string reason;
}