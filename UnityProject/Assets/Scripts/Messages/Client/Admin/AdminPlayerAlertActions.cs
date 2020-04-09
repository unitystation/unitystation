using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		ActionRequested = reader.ReadInt32();
		RoundTimeOfIncident = reader.ReadString();
		PerpNetID = reader.ReadUInt32();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteInt32(ActionRequested);
		writer.WriteString(RoundTimeOfIncident);
		writer.WriteUInt32(PerpNetID);
		writer.WriteString(AdminToken);
	}
}
