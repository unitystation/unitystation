using Mirror;

namespace Messages.Client.VariableViewer
{
	public class RequestChangeVariableNetMessage : ClientMessage<RequestChangeVariableNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string newValue;
			public ulong PageID;
			public bool IsNewBookshelf;
			public bool SendToClient;
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
			global::VariableViewer.RequestChangeVariable(msg.PageID, msg.newValue, msg.SendToClient, SentByPlayer.GameObject, msg.AdminId);

			Logger.Log($"Admin {admin.name} changed variable {msg.PageID} (in VV) with a new value of: {msg.newValue} ",
				Category.Admin);
		}


		public static NetMessage Send(ulong _PageID, string _newValue ,bool InSendToClient , string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.PageID = _PageID;
			msg.newValue = _newValue;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;
			msg.SendToClient = InSendToClient;

			Send(msg);
			return msg;
		}
	}
}