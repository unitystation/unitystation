using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminCheckAdminMessages : ClientMessage
{
	public class AdminCheckAdminMessagesNetMessage : ActualMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	public override void Process(ActualMessage msg)
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
