using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminReplyMessage : ClientMessage
{
	public string Message;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(Message, SentByPlayer.UserId);
	}

	public static AdminReplyMessage Send(string message)
	{
		AdminReplyMessage msg = new AdminReplyMessage
		{
			Message = message
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Message = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Message);
	}
}
