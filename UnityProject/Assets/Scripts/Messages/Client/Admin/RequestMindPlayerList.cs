using Messages.Server.AdminTools;
using Mirror;
using UnityEngine;



namespace Messages.Client.Admin
{
	/// <summary>
	///     Request admin page data from the server
	/// </summary>
	public class RequestMindPlayerList : ClientMessage<RequestMindPlayerList.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (HasPermission(TAG.PLAYER_INFO, true) == false) return;

			AdminMindListRefreshMessage.Send(SentByPlayer.GameObject, SentByPlayer.AccountId);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
