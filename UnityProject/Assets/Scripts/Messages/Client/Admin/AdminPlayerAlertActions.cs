using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Messages.Client;

public class AdminPlayerAlertActions: ClientMessage
{
	public int ActionRequested;
	public string RoundTimeOfIncident;
	public uint PerpNetID;
	public string AdminToken;

	public override void Process()
	{
		UIManager.Instance.playerAlerts.ServerProcessActionRequest(SentByPlayer.UserId, (PlayerAlertActions)ActionRequested,
			RoundTimeOfIncident, PerpNetID, AdminToken);
	}

	public static AdminPlayerAlertActions Send(PlayerAlertActions actionRequested, string roundTimeOfIncident, uint perpId, string adminToken)
	{
		AdminPlayerAlertActions msg = new AdminPlayerAlertActions
		{
			ActionRequested = (int)actionRequested,
			RoundTimeOfIncident = roundTimeOfIncident,
			PerpNetID = perpId,
			AdminToken = adminToken
		};
		msg.Send();
		return msg;
	}
}
