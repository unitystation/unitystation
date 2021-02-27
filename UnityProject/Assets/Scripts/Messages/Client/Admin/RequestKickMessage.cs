using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestKickMessage : ClientMessage<RequestKickMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToKick;
			public string Reason;
			public bool IsBan;
			public int BanMinutes;
			public bool AnnounceBan;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		void VerifyAdminStatus(NetMessage msg)
		{
			var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (player != null)
			{
				PlayerList.Instance.ProcessKickRequest(msg.Userid, msg.UserToKick, msg.Reason, msg.IsBan, msg.BanMinutes, msg.AnnounceBan);
			}
		}

		public static NetMessage Send(string userId, string adminToken, string userIDToKick, string reason,
			bool ban = false, int banminutes = 0, bool announceBan = true)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToKick = userIDToKick,
				Reason = reason,
				IsBan = ban,
				BanMinutes = banminutes,
				AnnounceBan = announceBan
			};

			Send(msg);
			return msg;
		}
	}
}
