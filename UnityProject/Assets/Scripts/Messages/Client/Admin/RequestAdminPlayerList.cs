using Messages.Client;
using Messages.Server.AdminTools;
using Mirror;

namespace Messages.Client.Admin
{
	/// <summary>
	///     Request admin page data from the server
	/// </summary>
	public class RequestAdminPlayerList : ClientMessage<RequestAdminPlayerList.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		void VerifyAdminStatus(NetMessage msg)
		{
			var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
			if (player == null)
			{
				player = PlayerList.Instance.GetMentor(msg.Userid, msg.AdminToken);
				if(player == null)
					return;
			}

			AdminPlayerListRefreshMessage.Send(player, msg.Userid);
		}

		public static NetMessage Send(string userId, string adminToken)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken
			};

			Send(msg);
			return msg;
		}
	}
}
