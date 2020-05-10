using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class RequestRespawnPlayer : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string UserToRespawn;
	public string OccupationToRespawn;

	public override void Process()
	{
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			var deadPlayer = PlayerList.Instance.GetByUserID(UserToRespawn);
			if (deadPlayer.Script.playerHealth == null)
			{
				TryRespawn(deadPlayer, OccupationToRespawn);
				return;
			}

			if (deadPlayer.Script.playerHealth.IsDead)
			{
				TryRespawn(deadPlayer, OccupationToRespawn);
			}
		}
	}

	void TryRespawn(ConnectedPlayer deadPlayer, string occupation = null)
	{
		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
			$"{PlayerList.Instance.GetByUserID(Userid).Name} respawned dead player {deadPlayer.Name} as {occupation}", Userid);
		deadPlayer.Script.playerNetworkActions.ServerRespawnPlayer(occupation);
	}

	public static RequestRespawnPlayer Send(string userId, string adminToken, string userIDToRespawn)
	{
		RequestRespawnPlayer msg = new RequestRespawnPlayer
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToRespawn = userIDToRespawn,
		};
		msg.Send();
		return msg;
	}

	public static RequestRespawnPlayer SendAdminJob(string userId, string adminToken, string userIDToRespawn,
		Occupation occupation)
	{
		RequestRespawnPlayer msg = new RequestRespawnPlayer
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToRespawn = userIDToRespawn,
			OccupationToRespawn = occupation.name
		};

		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToRespawn = reader.ReadString();
		OccupationToRespawn = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToRespawn);
		writer.WriteString(OccupationToRespawn);
	}
}
