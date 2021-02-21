using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminCheckAdminMessages : ClientMessage
{
	public struct AdminCheckAdminMessagesNetMessage : NetworkMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminCheckAdminMessagesNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminCheckAdminMessagesNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerGetUnreadMessages(newMsg.PlayerId, newMsg.CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckAdminMessagesNetMessage Send(string playerId, int currentCount)
	{
		AdminCheckAdminMessagesNetMessage msg = new AdminCheckAdminMessagesNetMessage
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};

		new AdminCheckAdminMessages().Send(msg);
		return msg;
	}
}
