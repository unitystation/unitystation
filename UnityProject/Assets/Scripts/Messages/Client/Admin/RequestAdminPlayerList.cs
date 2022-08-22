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
			if (IsFromAdmin() == false && PlayerList.Instance.IsMentor(SentByPlayer.UserId) == false) return;

			AdminPlayerListRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.UserId);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
