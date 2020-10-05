using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AdminTools;
using InGameEvents;

public class AdminToolRefreshMessage : ServerMessage
{
	public string JsonData;
	public uint Recipient;

	public override void Process()
	{
		LoadNetworkObject(Recipient);
		var adminPageData = JsonUtility.FromJson<AdminPageRefreshData>(JsonData);

		var pages = GameObject.FindObjectsOfType<AdminPage>();
		foreach (var g in pages)
		{
			g.GetComponent<AdminPage>().OnPageRefresh(adminPageData);
		}
	}

	public static AdminToolRefreshMessage Send(GameObject recipient, string adminID)
	{
		//Gather the data:
		var pageData = new AdminPageRefreshData();

		//Game Mode Information:
		pageData.availableGameModes = GameManager.Instance.GetAvailableGameModeNames();
		pageData.isSecret = GameManager.Instance.SecretGameMode;
		pageData.currentGameMode = GameManager.Instance.GetGameModeName(true);
		pageData.nextGameMode = GameManager.Instance.NextGameMode;

		//Event Manager
		pageData.randomEventsAllowed = InGameEventsManager.Instance.RandomEventsAllowed;

		//Round Manager
		pageData.nextMap = SubSceneManager.AdminForcedMainStation;
		pageData.nextAwaySite = SubSceneManager.AdminForcedAwaySite;
		pageData.allowLavaLand = SubSceneManager.AdminAllowLavaland;
		pageData.alertLevel = GameManager.Instance.CentComm.CurrentAlertLevel.ToString();

		//Centcom
		pageData.blockCall = GameManager.Instance.PrimaryEscapeShuttle.blockCall;
		pageData.blockRecall = GameManager.Instance.PrimaryEscapeShuttle.blockRecall;

		//Player list info:
		pageData.players = GetAllPlayerStates(adminID);

		var data = JsonUtility.ToJson(pageData);

		AdminToolRefreshMessage  msg =
			new AdminToolRefreshMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		msg.SendTo(recipient);
		return msg;
	}

	private static List<AdminPlayerEntryData> GetAllPlayerStates(string adminID)
	{
		var playerList = new List<AdminPlayerEntryData>();
		if (string.IsNullOrEmpty(adminID)) return playerList;
		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player == null) continue;
			if (player.Connection == null) continue;

			var entry = new AdminPlayerEntryData();
			entry.name = player.Name;
			entry.uid = player.UserId;
			entry.currentJob = player.Job.ToString();
			entry.accountName = player.Username;
			entry.ipAddress = player.Connection.address;
			if (player.Script != null && player.Script.playerHealth != null)
			{
				entry.isAlive = player.Script.playerHealth.ConsciousState != ConsciousState.DEAD;
			} else
			{
				entry.isAdmin = false;
			}
			entry.isAntag = PlayerList.Instance.AntagPlayers.Contains(player);
			entry.isAdmin = PlayerList.Instance.IsAdmin(player.UserId);
			entry.isOnline = true;

			playerList.Add(entry);
		}

		return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
	}
}
