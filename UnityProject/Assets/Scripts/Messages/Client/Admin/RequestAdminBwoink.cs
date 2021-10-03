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
			if (IsFromAdmin())
			{
				var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
				foreach (var r in recipient)
				{
					AdminBwoinkMessage.Send(r.GameObject, SentByPlayer.UserId, $"<color=red>{msg.Message}</color>");
					UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(
							msg.Message, msg.UserToBwoink, SentByPlayer.UserId);
				}
			}
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
