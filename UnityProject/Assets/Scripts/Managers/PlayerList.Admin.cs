using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using UnityEngine;

/// <summary>
/// Admin Controller for players
/// </summary>
public partial class PlayerList
{
	private FileSystemWatcher adminListWatcher;
	private List<string> adminUsers = new List<string>();
	private string adminsPath;

	[Server]
	void InitAdminController()
	{
		adminsPath = Path.Combine(Application.streamingAssetsPath, "admin", "admins.txt");

		if (!File.Exists(adminsPath))
		{
			File.CreateText(adminsPath);
		}

		adminListWatcher = new FileSystemWatcher();
		adminListWatcher.Path = Path.GetDirectoryName(adminsPath);
		adminListWatcher.Filter = Path.GetFileName(adminsPath);
		adminListWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite;
		adminListWatcher.Changed += LoadCurrentAdmins;
		adminListWatcher.EnableRaisingEvents = true;

		LoadCurrentAdmins();
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

	public bool ValidatePlayer(string clientID, string username,
		string userid, int clientVersion, ConnectedPlayer playerConn)
	{
		if (clientVersion != GameData.BuildNumber)
		{
			StartCoroutine(KickPlayer(playerConn, $"Invalid Client Version! You need version {GameData.BuildNumber}"));
			return false;
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