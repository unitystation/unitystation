using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Messages.Client;

public class AdminPlayerAlertActions : ClientMessage
{
	public class AdminPlayerAlertActionsNetMessage : ActualMessage
	{
		public int ActionRequested;
		public string RoundTimeOfIncident;
		public uint PerpNetID;
		public string AdminToken;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminPlayerAlertActionsNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.playerAlerts.ServerProcessActionRequest(SentByPlayer.UserId, (PlayerAlertActions)newMsg.ActionRequested,
			newMsg.RoundTimeOfIncident, newMsg.PerpNetID, newMsg.AdminToken);
	}

	public static AdminPlayerAlertActionsNetMessage Send(PlayerAlertActions actionRequested, string roundTimeOfIncident, uint perpId, string adminToken)
	{
		AdminPlayerAlertActionsNetMessage msg = new AdminPlayerAlertActionsNetMessage
		{
			ActionRequested = (int)actionRequested,
			RoundTimeOfIncident = roundTimeOfIncident,
			PerpNetID = perpId,
			AdminToken = adminToken
		};
		new AdminPlayerAlertActions().Send(msg);
		return msg;
	}
}
