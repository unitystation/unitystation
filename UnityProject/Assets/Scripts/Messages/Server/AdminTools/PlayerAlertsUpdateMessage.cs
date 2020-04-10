using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class PlayerAlertsUpdateMessage : ServerMessage
{
	public string JsonData;
	public bool IsSingleEntry;

	public override void Process()
	{
		if (IsSingleEntry)
		{
			UIManager.Instance.playerAlerts.ClientUpdateSingleEntry(JsonUtility.FromJson<PlayerAlertData>(JsonData));
		}
		else
		{
			UIManager.Instance.playerAlerts.ClientUpdateAlertLog(JsonData);
		}
	}

	public static PlayerAlertsUpdateMessage SendSingleEntryToAdmins(PlayerAlertData alertMessage)
	{
		PlayerAlertsUpdateMessage  msg =
			new PlayerAlertsUpdateMessage
			{
				JsonData = JsonUtility.ToJson(alertMessage),
				IsSingleEntry = true
			};

		msg.SendToAdmins();
		return msg;
	}

	public static PlayerAlertsUpdateMessage SendLogUpdateToAdmin(NetworkConnection requestee, PlayerAlertsUpdate update)
	{
		PlayerAlertsUpdateMessage msg =
			new PlayerAlertsUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
				IsSingleEntry = false
			};

		msg.SendTo(requestee);
		return msg;
	}
}
