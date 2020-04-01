using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class PlayerAlertsUpdateMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.PlayerAlertsUpdateMessage;
	public string JsonData;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		UIManager.Instance.playerAlerts.ClientUpdateAlertLog(JsonData);
	}

	public static PlayerAlertsUpdateMessage SendSingleEntryToAdmins(PlayerAlertData alertMessage)
	{
		PlayerAlertsUpdate update = new PlayerAlertsUpdate();
		update.playerAlerts.Add(alertMessage);
		PlayerAlertsUpdateMessage  msg =
			new PlayerAlertsUpdateMessage  {JsonData = JsonUtility.ToJson(update) };

		msg.SendToAdmins();
		return msg;
	}

	public static PlayerAlertsUpdateMessage SendLogUpdateToAdmin(NetworkConnection requestee, PlayerAlertsUpdate update)
	{
		PlayerAlertsUpdateMessage msg =
			new PlayerAlertsUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
			};

		msg.SendTo(requestee);
		return msg;
	}
}
