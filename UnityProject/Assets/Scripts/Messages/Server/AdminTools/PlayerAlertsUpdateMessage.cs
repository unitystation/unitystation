using AdminTools;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class PlayerAlertsUpdateMessage : ServerMessage<PlayerAlertsUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public bool IsSingleEntry;
		}

		public override void Process(NetMessage msg)
		{
			if (msg.IsSingleEntry)
			{
				UIManager.Instance.playerAlerts.ClientUpdateSingleEntry(JsonUtility.FromJson<PlayerAlertData>(msg.JsonData));
			}
			else
			{
				UIManager.Instance.playerAlerts.ClientUpdateAlertLog(msg.JsonData);
			}
		}

		public static NetMessage SendSingleEntryToAdmins(PlayerAlertData alertMessage)
		{
			NetMessage  msg =
				new NetMessage
				{
					JsonData = JsonUtility.ToJson(alertMessage),
					IsSingleEntry = true
				};

			SendToAdmins(msg);
			return msg;
		}

		public static NetMessage SendLogUpdateToAdmin(NetworkConnection requestee, PlayerAlertsUpdate update)
		{
			NetMessage msg =
				new NetMessage
				{
					JsonData = JsonUtility.ToJson(update),
					IsSingleEntry = false
				};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
