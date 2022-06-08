using Mirror;
using Messages.Server.AdminTools;


namespace Messages.Client.Admin
{
	public class RequestAdminBwoink : ClientMessage<RequestAdminBwoink.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserToBwoink;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;
			
			if (PlayerList.Instance.TryGetByUserID(msg.UserToBwoink, out var recipient) == false) return;
			
			AdminBwoinkMessage.Send(recipient.GameObject, SentByPlayer.UserId, $"<color=red>{msg.Message}</color>");
			UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(
					msg.Message, msg.UserToBwoink, SentByPlayer.UserId);
		}

		public static NetMessage Send(string userIDToBwoink, string message)
		{
			NetMessage msg = new NetMessage
			{
				UserToBwoink = userIDToBwoink,
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
