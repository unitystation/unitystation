using Mirror;
using Messages.Server.AdminTools;


namespace Messages.Client.Admin
{
	public class RequestMentorBwoink : ClientMessage<RequestMentorBwoink.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserToBwoink;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin() == false && PlayerList.Instance.IsMentor(SentByPlayer.UserId) == false) return;

			if (PlayerList.Instance.TryGetByUserID(msg.UserToBwoink, out var recipient) == false) return;

			MentorBwoinkMessage.Send(recipient.GameObject, SentByPlayer.UserId, $"<color=#6400FF>{SentByPlayer.Username}: { GameManager.Instance.RoundTime.ToString(@"hh\:mm\:ss") + " - " + msg.Message}</color>");
			UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg.Message, recipient, SentByPlayer);
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
