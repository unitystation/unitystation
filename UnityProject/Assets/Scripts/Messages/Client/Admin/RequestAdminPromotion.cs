using Mirror;


namespace Messages.Client.Admin
{
	public class RequestAdminPromotion : ClientMessage<RequestAdminPromotion.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string UserToPromote;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			PlayerList.Instance.ProcessAdminEnableRequest(SentByPlayer.UserId, msg.UserToPromote);
			var user = PlayerList.Instance.GetByUserID(msg.UserToPromote);
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
					$"{SentByPlayer.Username} made {user.Name} an admin. Users ID is: {msg.UserToPromote}", SentByPlayer.UserId);
		}

		public static NetMessage Send(string userIDToPromote)
		{
			NetMessage msg = new NetMessage
			{
				UserToPromote= userIDToPromote,
			};

			Send(msg);
			return msg;
		}
	}
}
