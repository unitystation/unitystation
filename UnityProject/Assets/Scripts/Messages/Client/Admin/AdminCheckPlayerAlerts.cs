using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminCheckPlayerAlerts : ClientMessage
{
	public struct AdminCheckPlayerAlertsNetMessage : NetworkMessage
	{
		public string PlayerId;
		public int CurrentCount;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminCheckPlayerAlertsNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminCheckPlayerAlertsNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
