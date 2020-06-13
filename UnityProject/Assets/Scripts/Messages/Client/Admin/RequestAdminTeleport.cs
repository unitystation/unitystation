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
		PlayerSync userToTeleport = null;

		foreach (var player in PlayerList.Instance.AllPlayers)
		{
			if (player.UserId == UserToTeleport)
			{
				userToTeleport = player.Script.PlayerSync;

				break;
			}
		}

		if (userToTeleport == null) return;

		userToTeleport.SetPosition(SentByPlayer.Script.AssumedWorldPos);
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

		playerScript.PlayerSync.SetPosition(userToTeleportTo.AssumedWorldPos);
	}

	public static RequestAdminTeleport Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, bool isAdminToPlayer)
	{
		RequestAdminTeleport msg = new RequestAdminTeleport
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToTeleport = userToTeleport,
			UserToTeleportTo = userToTelportTo,
			IsAdminToPlayer = isAdminToPlayer
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
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToTeleport);
		writer.WriteString(UserToTeleportTo);
		writer.WriteBoolean(IsAdminToPlayer);
	}
}
