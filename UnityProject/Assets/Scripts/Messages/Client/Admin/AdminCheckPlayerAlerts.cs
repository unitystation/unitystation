using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminCheckPlayerAlerts : ClientMessage
{
	public class AdminCheckPlayerAlertsNetMessage : NetworkMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as AdminCheckPlayerAlertsNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.playerAlerts.ServerRequestEntries(newMsg.PlayerId, newMsg.CurrentCount, SentByPlayer.Connection);
	}

	public static AdminCheckPlayerAlertsNetMessage Send(string playerId, int currentCount)
	{
		AdminCheckPlayerAlertsNetMessage msg = new AdminCheckPlayerAlertsNetMessage
		{
			PlayerId = playerId,
			CurrentCount = currentCount
		};
		new AdminCheckPlayerAlerts().Send(msg);
		return msg;
	}
}
