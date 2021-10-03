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
			VerifyMentorStatus(msg);
		}

		private void VerifyMentorStatus(NetMessage msg)
		{
			if (IsFromAdmin() == false && PlayerList.Instance.IsMentor(SentByPlayer.UserId) == false) return;

			var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
			foreach (var r in recipient)
			{
				MentorBwoinkMessage.Send(r.GameObject, SentByPlayer.UserId, $"<color=#6400FF>{msg.Message}</color>");
				UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg.Message, msg.UserToBwoink, SentByPlayer.UserId);
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
