using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class PlayerAlertsUpdateMessage : ServerMessage
{
	public class PlayerAlertsUpdateMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public bool IsSingleEntry;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as PlayerAlertsUpdateMessageNetMessage;
		if(newMsg == null) return;

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
