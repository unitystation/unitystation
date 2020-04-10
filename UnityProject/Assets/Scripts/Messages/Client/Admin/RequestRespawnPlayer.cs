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
				TryRespawn(deadPlayer);
				return;
			}

			if (deadPlayer.Script.playerHealth.IsDead)
			{
				TryRespawn(deadPlayer);
				return;
			}
		}
	}

	void TryRespawn(ConnectedPlayer deadPlayer)
	{
		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
			$"{PlayerList.Instance.GetByUserID(Userid).Name} respawned dead player {deadPlayer.Name}", Userid);
		deadPlayer.Script.playerNetworkActions.ServerRespawnPlayer();
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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToRespawn = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToRespawn);
	}
}
