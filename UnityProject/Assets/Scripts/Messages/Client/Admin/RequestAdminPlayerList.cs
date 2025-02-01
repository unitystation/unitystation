using Mirror;
using Messages.Server.AdminTools;


namespace Messages.Client.Admin
{
	/// <summary>
	///     Request admin page data from the server
	/// </summary>
	public class RequestAdminPlayerList : ClientMessage<RequestAdminPlayerList.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (HasPermission(TAG.PLAYER_INFO, true) == false) return;

			var ShowIP = HasPermission(TAG.PLAYER_INFO_IP, false);

			AdminPlayerListRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.AccountId, ShowIP);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
