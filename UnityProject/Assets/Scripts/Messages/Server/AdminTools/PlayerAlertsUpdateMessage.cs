using AdminTools;
using Mirror;
using Newtonsoft.Json;
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
				UIManager.Instance.playerAlerts.ClientUpdateSingleEntry(JsonConvert.DeserializeObject<PlayerAlertData>(msg.JsonData));
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
					JsonData = JsonConvert.SerializeObject(alertMessage),
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
					JsonData = JsonConvert.SerializeObject(update),
					IsSingleEntry = false
				};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
