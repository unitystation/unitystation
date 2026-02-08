using Mirror;
using UnityEngine;
using US13.Core.Chat;

namespace US13.Messages.Server.AdminTools
{
	public class PrayerBwoinkMessage : ServerMessage<PrayerBwoinkMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string AdminUID;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			Chat.AddPrayerPrivMsg(msg.Message);
		}

		public static NetMessage  Send(GameObject recipient, string adminUid, string message)
		{
			NetMessage  msg = new NetMessage
			{
				AdminUID = adminUid,
				Message = message
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
