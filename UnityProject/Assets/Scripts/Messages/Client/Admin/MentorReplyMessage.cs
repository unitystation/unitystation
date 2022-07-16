using Mirror;

namespace Messages.Client.Admin
{
	public class MentorReplyMessage : ClientMessage<MentorReplyMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg.Message, SentByPlayer);
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
