using Mirror;
using Messages.Server.AdminTools;


namespace Messages.Client.Admin
{
	public class RequestPrayerBwoink : ClientMessage<RequestPrayerBwoink.NetMessage>
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

			PrayerBwoinkMessage.Send(recipient.GameObject, SentByPlayer.AccountId, $"<i><color=yellow>{msg.Message}</color></i>");
			UIManager.Instance.adminChatWindows.playerPrayerWindow.ServerAddChatRecord(msg.Message, recipient, SentByPlayer);
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
