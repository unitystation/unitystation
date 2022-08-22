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
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg.Message, SentByPlayer.UserId);
			}
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
