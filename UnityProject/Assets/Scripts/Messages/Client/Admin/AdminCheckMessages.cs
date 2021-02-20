using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminCheckMessages : ClientMessage
{
	public class AdminCheckMessagesNetMessage : ActualMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminCheckMessagesNetMessage;
		if(newMsg == null) return;

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
