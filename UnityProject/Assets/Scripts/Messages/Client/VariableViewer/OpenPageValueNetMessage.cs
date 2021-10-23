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
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestOpenPageValue(msg.PageID, msg.SentenceID, msg.ISSentence, msg.iskey, SentByPlayer.GameObject);
		}

		public static NetMessage Send(ulong _PageID, uint _SentenceID, bool Sentenceis = false, bool _iskey = false)
		{
			NetMessage msg = new NetMessage
			{
				PageID = _PageID,
				SentenceID = _SentenceID,
				ISSentence = Sentenceis,
				iskey = _iskey
			};

			Send(msg);
			return msg;
		}
	}
}
