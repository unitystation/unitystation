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
			if (IsFromAdmin() == false) return;

			if (PlayerList.Instance.TryGetByUserID(msg.UserToBwoink, out var recipient) == false) return;

			AdminBwoinkMessage.Send(recipient.GameObject, SentByPlayer.UserId, $"<color=red>{SentByPlayer.Username}: {GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + msg.Message}</color>");
			UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(msg.Message, recipient, SentByPlayer);
		}

		public static NetMessage Send(string userIDToBwoink, string message)
		{
			NetMessage msg = new()
			{
				UserToBwoink = userIDToBwoink,
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
