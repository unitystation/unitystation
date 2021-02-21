using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class PlayerAlertsUpdateMessage : ServerMessage
{
	public struct PlayerAlertsUpdateMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public bool IsSingleEntry;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public PlayerAlertsUpdateMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PlayerAlertsUpdateMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (newMsg.IsSingleEntry)
		{
			UIManager.Instance.playerAlerts.ClientUpdateSingleEntry(JsonUtility.FromJson<PlayerAlertData>(newMsg.JsonData));
		}
		else
		{
			UIManager.Instance.playerAlerts.ClientUpdateAlertLog(newMsg.JsonData);
		}
	}

	public static PlayerAlertsUpdateMessageNetMessage SendSingleEntryToAdmins(PlayerAlertData alertMessage)
	{
		PlayerAlertsUpdateMessageNetMessage  msg =
			new PlayerAlertsUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(alertMessage),
				IsSingleEntry = true
			};

		new PlayerAlertsUpdateMessage().SendToAdmins(msg);
		return msg;
	}

	public static PlayerAlertsUpdateMessageNetMessage SendLogUpdateToAdmin(NetworkConnection requestee, PlayerAlertsUpdate update)
	{
		PlayerAlertsUpdateMessageNetMessage msg =
			new PlayerAlertsUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(update),
				IsSingleEntry = false
			};

		new PlayerAlertsUpdateMessage().SendTo(requestee, msg);
		return msg;
	}
}
