using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Mirror;
using UnityEngine;

public class RequestAdminTeleport : ClientMessage
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

	public override void Process()
	{
		switch (OpperationNumber)
		{
			case OpperationList.AdminToPlayer:
				DoAdminToPlayerTeleport();
				return;
			case OpperationList.PlayerToAdmin:
				DoPlayerToAdminTeleport();
				return;
			case OpperationList.AllPlayersToPlayer:
				DoAllPlayersToPlayerTeleport();
				return;
		}
	}

	private void DoPlayerToAdminTeleport()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin == null) return;

		PlayerScript userToTeleport = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == UserToTeleport)
			{
				userToTeleport = player.Script;

				break;
			}
		}

		if (userToTeleport == null) return;

		var coord = new Vector3 {x = vectorX, y = vectorY, z = vectorZ };

		userToTeleport.PlayerSync.SetPosition(coord, true);

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{SentByPlayer.Username} teleported {userToTeleport.playerName} to themselves", Userid);
	}

	private void DoAdminToPlayerTeleport()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin == null) return;

		PlayerScript userToTeleportTo = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == UserToTeleportTo)
			{
				userToTeleportTo = player.Script;

				break;
			}
		}

		if (userToTeleportTo == null) return;

		var playerScript = SentByPlayer.Script;

		if (playerScript == null) return;

		playerScript.PlayerSync.SetPosition(userToTeleportTo.gameObject.AssumedWorldPosServer(), true);

		string msg;

		if (IsAghost)
		{
			msg = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a ghost";
		}
		else
		{
			msg = $"{SentByPlayer.Username} teleported to {userToTeleportTo.playerName} as a player";
		}

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, Userid);
	}

	private void DoAllPlayersToPlayerTeleport()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin == null) return;

		PlayerScript destinationPlayer = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == UserToTeleportTo)
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

			if (IsAghost)
			{
				var coord = new Vector3 { x = vectorX, y = vectorY, z = vectorZ };

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

		var msg = $"{SentByPlayer.Username} teleported all players to {destinationPlayer.playerName}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, Userid);
	}

	public static RequestAdminTeleport Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, OpperationList opperation, bool isAghost, Vector3 Coord)
	{
		RequestAdminTeleport msg = new RequestAdminTeleport
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
		msg.Send();
		return msg;
	}

	public enum OpperationList
	{
		AdminToPlayer = 1,
		PlayerToAdmin = 2,
		AllPlayersToPlayer = 3
	}
}
