using Messages.Server.AdminTools;
using Mirror;


namespace Messages.Client.Admin
{
	/// <summary>
	///     Request admin page data from the server
	/// </summary>
	public class RequestAdminPageRefresh : ClientMessage<RequestAdminPageRefresh.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin())
			{
				AdminToolRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.UserId);
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
