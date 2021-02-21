using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerListRefreshMessage : ServerMessage
{
	public class AdminPlayerListRefreshMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public uint Recipient;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminPlayerListRefreshMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.Recipient);
		var listData = JsonUtility.FromJson<AdminPlayersList>(newMsg.JsonData);

		foreach (var v in UIManager.Instance.adminChatWindows.playerListViews)
		{
			if (v.gameObject.activeInHierarchy)
			{
				v.ReceiveUpdatedPlayerList(listData);
			}
		}
	}

	public static AdminPlayerListRefreshMessageNetMessage Send(GameObject recipient, string adminID)
	{
		AdminPlayersList playerList = new AdminPlayersList();
		//Player list info:
		playerList.players = GetAllPlayerStates(adminID);

		var data = JsonUtility.ToJson(playerList);

		AdminPlayerListRefreshMessageNetMessage  msg =
			new AdminPlayerListRefreshMessageNetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

		new AdminPlayerListRefreshMessage().SendTo(recipient, msg);
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
