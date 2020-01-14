using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminToolRefreshMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminToolRefreshMessage;
	public string JsonData;
	public uint Recipient;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
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
		Dictionary<string, AdminPlayerEntryData> validationList = new Dictionary<string, AdminPlayerEntryData>();

		var checkMessages = PlayerList.Instance.CheckAdminInbox(adminID);
		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (validationList.ContainsKey(player.UserId)) continue;

			var entry = new AdminPlayerEntryData();
			entry.name = player.Name;
			entry.uid = player.UserId;
			entry.currentJob = player.Job.ToString();
			entry.isAlive = player.Script.IsGhost;
			entry.isAntag = PlayerList.Instance.AntagPlayers.Contains(player);
			entry.isAdmin = PlayerList.Instance.IsAdmin(player.UserId);
			entry.isOnline = player.Connection != null;

			foreach (var msg in checkMessages)
			{
				if (msg.fromUserid == entry.uid)
				{
					entry.newMessages.Add(msg);
				}
			}

			playerList.Add(entry);
			validationList.Add(entry.uid, entry);
		}

		return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
	}
}
