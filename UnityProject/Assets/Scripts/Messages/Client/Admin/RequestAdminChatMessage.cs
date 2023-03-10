using Mirror;


namespace Messages.Client.Admin
{
	public class RequestAdminChatMessage : ClientMessage<RequestAdminChatMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg.Message, SentByPlayer);
			}
		}

		public static NetMessage Send(string message)
		{
			NetMessage msg = new()
			{
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
