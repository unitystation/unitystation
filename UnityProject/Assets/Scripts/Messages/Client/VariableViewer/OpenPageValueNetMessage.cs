using Mirror;

namespace Messages.Client.VariableViewer
{
	public class OpenPageValueNetMessage : ClientMessage<OpenPageValueNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong PageID;
			public uint SentenceID;
			public bool ISSentence;
			public bool iskey;
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
			global::VariableViewer.RequestOpenPageValue(msg.PageID, msg.SentenceID, msg.ISSentence, msg.iskey, SentByPlayer.GameObject);
		}

		public static NetMessage Send(ulong _PageID, uint _SentenceID, string adminId, string adminToken,
			bool Sentenceis = false, bool _iskey = false)
		{
			NetMessage msg = new NetMessage();
			msg.PageID = _PageID;
			msg.SentenceID = _SentenceID;
			msg.ISSentence = Sentenceis;
			msg.iskey = _iskey;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}
	}
}
