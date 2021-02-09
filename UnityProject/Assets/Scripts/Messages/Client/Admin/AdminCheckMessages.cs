using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

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
}
