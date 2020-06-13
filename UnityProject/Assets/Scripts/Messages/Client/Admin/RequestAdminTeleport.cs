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
	public int OpperationNumber;
	public bool IsAghost;
	public float vectorX;
	public float vectorY;
	public float vectorZ;

	public override void Process()
	{
		if (OpperationNumber == 1)
		{
			DoAdminToPlayerTeleport();
		}
		else if (OpperationNumber == 2)
		{
			DoPlayerToAdminTeleport();
		}
		else if (OpperationNumber == 3)
		{
			DoAllPlayersToPlayerTeleport();
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
			else
			{
				userToTeleport.PlayerSync.SetPosition(destinationPlayer.gameObject.AssumedWorldPosServer(), true);
			}
		}

		var msg = $"{SentByPlayer.Username} teleported all players to {destinationPlayer.playerName}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, Userid);
	}

	public static RequestAdminTeleport Send(string userId, string adminToken, string userToTeleport, string userToTelportTo, int opperationNumber, bool isAghost, Vector3 Coord)
	{
		RequestAdminTeleport msg = new RequestAdminTeleport
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToTeleport = userToTeleport,
			UserToTeleportTo = userToTelportTo,
			OpperationNumber = opperationNumber,
			IsAghost = isAghost,
			vectorX = Coord.x,
			vectorY = Coord.y,
			vectorZ = Coord.z
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
		OpperationNumber = reader.ReadInt32();
		IsAghost = reader.ReadBoolean();
		vectorX = reader.ReadSingle();
		vectorY = reader.ReadSingle();
		vectorZ = reader.ReadSingle();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToTeleport);
		writer.WriteString(UserToTeleportTo);
		writer.WriteInt32(OpperationNumber);
		writer.WriteBoolean(IsAghost);
		writer.WriteSingle(vectorX);
		writer.WriteSingle(vectorY);
		writer.WriteSingle(vectorZ);
	}
}
