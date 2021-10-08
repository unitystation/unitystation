using Mirror;


namespace Messages.Client.Admin
{
	public class RequestKickMessage : ClientMessage<RequestKickMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
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

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				PlayerList.Instance.ProcessKickRequest(
						SentByPlayer.UserId, msg.UserToKick, msg.Reason, msg.IsBan, msg.BanMinutes, msg.AnnounceBan);
			}
		}

		public static NetMessage Send(string userIDToKick, string reason, bool ban = false, int banminutes = 0, bool announceBan = true)
		{
			NetMessage msg = new NetMessage
			{
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
