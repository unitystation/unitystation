using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminCheckAdminMessages : ClientMessage
{
	public class AdminCheckAdminMessagesNetMessage : NetworkMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as AdminCheckAdminMessagesNetMessage;
		if(newMsg == null) return;

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
