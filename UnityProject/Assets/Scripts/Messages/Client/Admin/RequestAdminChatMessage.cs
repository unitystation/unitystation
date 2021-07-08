using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestAdminChatMessage : ClientMessage<RequestAdminChatMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string Message;
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
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg.Message, msg.Userid);
			}
		}

		public static NetMessage Send(string userId, string adminToken, string message)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				Message = message
			};

			Send(msg);
			return msg;
		}
	}
}
