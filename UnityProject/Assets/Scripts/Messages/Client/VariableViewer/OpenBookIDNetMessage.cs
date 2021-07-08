using Mirror;

namespace Messages.Client.VariableViewer
{
	public class OpenBookIDNetMessage : ClientMessage<OpenBookIDNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong BookID;
			public string AdminId;
			public string AdminToken;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		void ValidateAdmin(NetMessage msg)
		{
			var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
			if (admin == null) return;
			global::VariableViewer.RequestSendBook(msg.BookID, SentByPlayer.GameObject);
		}


		public static NetMessage Send(ulong BookID, string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.BookID = BookID;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}
	}
}
