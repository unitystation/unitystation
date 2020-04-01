using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminPlayerAlertActions: ClientMessage
{
	public override short MessageType => (short) MessageTypes.AdminPlayerAlertActions;

	public int ActionRequested;
	public string RoundTimeOfIncident;
	public uint PerpNetID;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		UIManager.Instance.playerAlerts.ServerProcessActionRequest(SentByPlayer.UserId, (PlayerAlertActions)ActionRequested,
			RoundTimeOfIncident, PerpNetID);
	}

	public static AdminPlayerAlertActions Send(PlayerAlertActions actionRequested, string roundTimeOfIncident, uint perpId)
	{
		AdminPlayerAlertActions msg = new AdminPlayerAlertActions
		{
			ActionRequested = (int)actionRequested,
			RoundTimeOfIncident = roundTimeOfIncident,
			PerpNetID = perpId
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
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteInt32(ActionRequested);
		writer.WriteString(RoundTimeOfIncident);
		writer.WriteUInt32(PerpNetID);
	}
}
