using Mirror;
using US13.Messages.Server.AdminTools;

namespace US13.Messages.Client.Admin
{
	/// <summary>
	///     Request admin page data from the server
	/// </summary>
	public class RequestAdminPageRefresh : ClientMessage<RequestAdminPageRefresh.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_INFO))
			{
				AdminToolRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.AccountId, HasPermission(TAG.PLAYER_INFO_IP, false));
			}
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
