using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerListRefreshMessage : ServerMessage
{
	public string JsonData;
	public uint Recipient;

	public override void Process()
	{
		LoadNetworkObject(Recipient);
		var listData = JsonUtility.FromJson<AdminPlayersList>(JsonData);

		foreach (var v in UIManager.Instance.adminChatWindows.playerListViews)
		{
			if (v.gameObject.activeInHierarchy)
			{
				v.ReceiveUpdatedPlayerList(listData);
			}
		}
	}

	public static AdminPlayerListRefreshMessage Send(GameObject recipient, string adminID)
	{
		AdminPlayersList playerList = new AdminPlayersList();
		//Player list info:
		playerList.players = GetAllPlayerStates(adminID);

		var data = JsonUtility.ToJson(playerList);

		AdminPlayerListRefreshMessage  msg =
			new AdminPlayerListRefreshMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

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
			if (player.Connection != null)
			{
				entry.ipAddress = player.Connection.address;
				if (player.Script != null && player.Script.playerHealth != null)
				{
					entry.isAlive = player.Script.playerHealth.ConsciousState != ConsciousState.DEAD;
				}
				else
				{
					entry.isAdmin = false;
				}
				entry.isOnline = true;
				entry.isAntag = PlayerList.Instance.AntagPlayers.Contains(player);
				entry.isAdmin = PlayerList.Instance.IsAdmin(player.UserId);
			}
			else
			{
				entry.isOnline = false;
			}

			playerList.Add(entry);
		}

		return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
	}
}
