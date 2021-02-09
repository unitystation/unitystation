using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminCheckAdminMessages : ClientMessage
{
	public string PlayerId;
	public int CurrentCount;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerGetUnreadMessages(PlayerId, CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckAdminMessages Send(string playerId, int currentCount)
	{
		AdminCheckAdminMessages msg = new AdminCheckAdminMessages
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};
		msg.Send();
		return msg;
	}
}
