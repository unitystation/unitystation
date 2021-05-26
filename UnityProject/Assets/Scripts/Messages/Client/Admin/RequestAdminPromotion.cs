using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestAdminPromotion : ClientMessage<RequestAdminPromotion.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public string UserToPromote;
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
				PlayerList.Instance.ProcessAdminEnableRequest(msg.Userid, msg.UserToPromote);
				var user = PlayerList.Instance.GetByUserID(msg.UserToPromote);
				UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{player.Player().Username} made {user.Name} an admin. Users ID is: {msg.UserToPromote}", msg.Userid);
			}
		}

		public static NetMessage Send(string userId, string adminToken, string userIDToPromote)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				UserToPromote= userIDToPromote,
			};

			Send(msg);
			return msg;
		}
	}
}
