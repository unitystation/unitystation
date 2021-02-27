using Messages.Client;
using Messages.Server.AdminTools;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestAdminBwoink : ClientMessage<RequestAdminBwoink.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToBwoink;
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
				var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
				foreach (var r in recipient)
				{
					AdminBwoinkMessage.Send(r.GameObject, msg.Userid, "<color=red>" + msg.Message + "</color>");
					UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(msg.Message, msg.UserToBwoink, msg.Userid);
				}
			}
		}

		public static NetMessage Send(string userId, string adminToken, string userIDToBwoink, string message)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToBwoink = userIDToBwoink,
				Message = message
			};
			Send(msg);
			return msg;
		}
	}
}
