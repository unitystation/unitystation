using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminReplyMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.AdminReplyMessage;

	public string AdminId;
	public string PlayerId;
	public string Message;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		if (!PlayerList.Instance.adminChatInbox.ContainsKey(AdminId))
		{
			PlayerList.Instance.adminChatInbox.Add(AdminId, new List<AdminChatMessage>());
		}

		Logger.Log($"{PlayerList.Instance.GetByUserID(PlayerId).Name} replied to Admin {PlayerList.Instance.GetByUserID(AdminId).Name} with: {Message}", Category.Admin);

		PlayerList.Instance.adminChatInbox[AdminId].Add(new AdminChatMessage
		{
			fromUserid = PlayerId,
			toUserid = AdminId,
			message = Message
		});
	}


	public static AdminReplyMessage Send(string adminId, string playerId, string message)
	{
		AdminReplyMessage msg = new AdminReplyMessage
		{
			AdminId = adminId,
			PlayerId = playerId,
			Message = message
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		AdminId = reader.ReadString();
		PlayerId = reader.ReadString();
		Message = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(AdminId);
		writer.WriteString(PlayerId);
		writer.WriteString(Message);
	}
}
