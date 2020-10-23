using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Messages.Client;
using Mirror;

public class MentorReplyMessage : ClientMessage
{
	public string Message;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(Message, SentByPlayer.UserId);
	}

	public static MentorReplyMessage Send(string message)
	{
		MentorReplyMessage msg = new MentorReplyMessage
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
