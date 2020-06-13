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
	public bool IsAdminToPlayer;
	public bool IsAghost;

	public override void Process()
	{
		if (IsAdminToPlayer)
		{
			DoAdminToPlayerTeleport();
		}
		else
		{
			DoPlayerToAdminTeleport();
		}
	}

	private void DoPlayerToAdminTeleport()
	{
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

		Vector3 pos;

		if (SentByPlayer.Script.pushPull == null)
		{
			pos = SentByPlayer.Script.WorldPos;
		}
		else
		{
			pos = SentByPlayer.Script.AssumedWorldPos;
		}

		userToTeleport.PlayerSync.SetPosition(pos);

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{SentByPlayer.Username} teleported {userToTeleport.playerName} to themselves", Userid);
	}

	private void DoAdminToPlayerTeleport()
	{
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

		Vector3 pos;

		if (userToTeleportTo.pushPull == null)
		{
			pos = userToTeleportTo.WorldPos;
		}
		else
		{
			pos = userToTeleportTo.AssumedWorldPos;
		}

		playerScript.PlayerSync.SetPosition(pos);

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

	public static RequestAdminTeleport Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, bool isAdminToPlayer, bool isAghost)
	{
		RequestAdminTeleport msg = new RequestAdminTeleport
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToTeleport = userToTeleport,
			UserToTeleportTo = userToTelportTo,
			IsAdminToPlayer = isAdminToPlayer,
			IsAghost = isAghost
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToTeleport = reader.ReadString();
		UserToTeleportTo = reader.ReadString();
		IsAdminToPlayer = reader.ReadBoolean();
		IsAghost = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToTeleport);
		writer.WriteString(UserToTeleportTo);
		writer.WriteBoolean(IsAdminToPlayer);
		writer.WriteBoolean(IsAghost);
	}
}
