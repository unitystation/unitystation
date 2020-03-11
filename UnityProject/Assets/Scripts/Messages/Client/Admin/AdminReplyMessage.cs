using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminReplyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.AdminReplyMessage;

	public string Message;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		Logger.Log($"{SentByPlayer.Name} replied to Admins: {Message}");

		PlayerList.Instance.ServerAddPlayerReply(Message, SentByPlayer.UserId);
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
