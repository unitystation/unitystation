using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Messages.Client;
using Mirror;

public class AdminPlayerAlertActions : ClientMessage
{
	public struct AdminPlayerAlertActionsNetMessage : NetworkMessage
	{
		public int ActionRequested;
		public string RoundTimeOfIncident;
		public uint PerpNetID;
		public string AdminToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminPlayerAlertActionsNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminPlayerAlertActionsNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
