using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminCheckMessages : ClientMessage
{
	public struct AdminCheckMessagesNetMessage : NetworkMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminCheckMessagesNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminCheckMessagesNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		UIManager.Instance.adminChatWindows.adminPlayerChat.ServerGetUnreadMessages(newMsg.PlayerId, newMsg.CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckMessagesNetMessage Send(string playerId, int currentCount)
	{
		AdminCheckMessagesNetMessage msg = new AdminCheckMessagesNetMessage
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};

		new AdminCheckMessages().Send(msg);
		return msg;
	}
}
