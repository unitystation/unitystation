using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestAdminTeleport : ClientMessage
{
	public struct RequestAdminTeleportNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToTeleport;
		public string UserToTeleportTo;
		public OpperationList OpperationNumber;
		public bool IsAghost;
		public float vectorX;
		public float vectorY;
		public float vectorZ;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestAdminTeleportNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestAdminTeleportNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		switch (newMsg.OpperationNumber)
		{
			case OpperationList.AdminToPlayer:
				DoAdminToPlayerTeleport(newMsg);
				return;
			case OpperationList.PlayerToAdmin:
				DoPlayerToAdminTeleport(newMsg);
				return;
			case OpperationList.AllPlayersToPlayer:
				DoAllPlayersToPlayerTeleport(newMsg);
				return;
		}
	}

	private void DoPlayerToAdminTeleport(RequestAdminTeleportNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (admin == null) return;

		PlayerScript userToTeleport = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == msg.UserToTeleport)
			{
				userToTeleport = player.Script;

				break;
			}
		}

		if (userToTeleport == null) return;

		var coord = new Vector3 {x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

		userToTeleport.PlayerSync.SetPosition(coord, true);

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{SentByPlayer.Username} teleported {userToTeleport.playerName} to themselves", msg.Userid);
	}

	private void DoAdminToPlayerTeleport(RequestAdminTeleportNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (admin == null) return;

		PlayerScript userToTeleportTo = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == msg.UserToTeleportTo)
			{
				userToTeleportTo = player.Script;

				break;
			}
		}

		if (userToTeleportTo == null) return;

		var playerScript = SentByPlayer.Script;

		if (playerScript == null) return;

		playerScript.PlayerSync.SetPosition(userToTeleportTo.gameObject.AssumedWorldPosServer(), true);

		string message;

		if (msg.IsAghost)
		{
			message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a ghost";
		}
		else
		{
			message = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a player";
		}

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(message, msg.Userid);
	}

	private void DoAllPlayersToPlayerTeleport(RequestAdminTeleportNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (admin == null) return;

		PlayerScript destinationPlayer = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == msg.UserToTeleportTo)
			{
				destinationPlayer = player.Script;

				break;
			}
		}

		if (destinationPlayer == null) return;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			PlayerScript userToTeleport = player.Script;

			if (userToTeleport == null) continue;

			if (msg.IsAghost)
			{
				var coord = new Vector3 { x = msg.vectorX, y = msg.vectorY, z = msg.vectorZ };

				userToTeleport.PlayerSync.SetPosition(coord, true);
			}
			else if (destinationPlayer.IsGhost)
			{
				//if the  destination player player is a ghost the system breaks as for some reason ghost position is not accurate on server.
				//To test for future reference: test coord on headless, works fine in editor.
				//if admin is ghost top condition is used as the admin can pass their position from client to server.
				return;
			}
			else
			{
				userToTeleport.PlayerSync.SetPosition(destinationPlayer.gameObject.AssumedWorldPosServer(), true);
			}
		}

		var stringMsg = $"{SentByPlayer.Username} teleported all players to {destinationPlayer.playerName}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(stringMsg, msg.Userid);
	}

	public static RequestAdminTeleportNetMessage Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, OpperationList opperation, bool isAghost, Vector3 Coord)
	{
		RequestAdminTeleportNetMessage msg = new RequestAdminTeleportNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToTeleport = userToTeleport,
			UserToTeleportTo = userToTelportTo,
			OpperationNumber = opperation,
			IsAghost = isAghost,
			vectorX = Coord.x,
			vectorY = Coord.y,
			vectorZ = Coord.z
		};
		new RequestAdminTeleport().Send(msg);
		return msg;
	}

	public enum OpperationList
	{
		AdminToPlayer = 1,
		PlayerToAdmin = 2,
		AllPlayersToPlayer = 3
	}
}
