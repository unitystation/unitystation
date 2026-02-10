using Mirror;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin
{
	public class PrayerReplyMessage : ClientMessage<PrayerReplyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.playerPrayerWindow.ServerAddChatRecord(msg.Message, SentByPlayer);
		}

		public static NetMessage Send(string message)
		{
			NetMessage msg = new NetMessage
			{
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
