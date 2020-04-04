using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class AdminCheckPlayerAlerts : ClientMessage
{
	public override short MessageType => (short) MessageTypes.RequestAdminPlayerAlerts;

	public string PlayerId;
	public int CurrentCount;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();
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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PlayerId = reader.ReadString();
		CurrentCount = reader.ReadInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(PlayerId);
		writer.WriteInt32(CurrentCount);
	}
}
