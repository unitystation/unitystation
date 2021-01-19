using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminCheckPlayerAlerts : ClientMessage
{
	public string PlayerId;
	public int CurrentCount;

	public override void Process()
	{
		UIManager.Instance.playerAlerts.ServerRequestEntries(PlayerId, CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckPlayerAlerts Send(string playerId, int currentCount)
	{
		AdminCheckPlayerAlerts msg = new AdminCheckPlayerAlerts
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};
		msg.Send();
		return msg;
	}
}
