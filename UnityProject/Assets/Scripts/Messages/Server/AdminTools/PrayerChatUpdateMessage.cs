using AdminTools;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class PrayerChatUpdateMessage : ServerMessage<PrayerChatUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public string PlayerId;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.playerPrayerWindow.ClientUpdateChatLog(msg.JsonData, msg.PlayerId);
		}

		public static NetMessage SendSinglePrayerEntryToAdmins(AdminChatMessage chatMessage, string playerId)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);

			NetMessage  msg = new NetMessage
			{
				JsonData = JsonConvert.SerializeObject(update),
				PlayerId = playerId
			};

			SendToAdmins(msg);
			return msg;
		}

		public static NetMessage SendPrayerLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update, string playerId)
		{
			NetMessage msg = new NetMessage
			{
				JsonData = JsonConvert.SerializeObject(update),
				PlayerId = playerId
			};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
