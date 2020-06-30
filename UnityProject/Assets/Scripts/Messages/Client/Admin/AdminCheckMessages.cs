using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminCheckMessages : ClientMessage
{
	public string PlayerId;
	public int CurrentCount;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.adminPlayerChat.ServerGetUnreadMessages(PlayerId, CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckMessages Send(string playerId, int currentCount)
	{
		AdminCheckMessages msg = new AdminCheckMessages
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PlayerId = reader.ReadString();
		CurrentCount = reader.ReadInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(PlayerId);
		writer.WriteInt32(CurrentCount);
	}
}
