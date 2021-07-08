using Messages.Server.AdminTools;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestMentorBwoink : ClientMessage<RequestMentorBwoink.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string MentorToken;
			public string UserToBwoink;
			public string Message;
		}

		public override void Process(NetMessage msg)
		{
			VerifyMentorStatus(msg);
		}

		void VerifyMentorStatus(NetMessage msg)
		{
			var player = PlayerList.Instance.GetMentor(msg.Userid, msg.MentorToken);
			if (player == null)
			{
				player = PlayerList.Instance.GetAdmin(msg.Userid, msg.MentorToken);
				if(player == null){
					//theoretically this shouldnt happen, and indicates someone might be tampering with the client.
					return;
				}
			}
			var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
			foreach (var r in recipient)
			{
				MentorBwoinkMessage.Send(r.GameObject, msg.Userid, "<color=#6400FF>" + msg.Message + "</color>");
				UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg.Message, msg.UserToBwoink, msg.Userid);
			}
		}

		public static NetMessage Send(string userId, string mentorToken, string userIDToBwoink, string message)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				MentorToken = mentorToken,
				UserToBwoink = userIDToBwoink,
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
