using Mirror;

namespace Messages.Client.VariableViewer
{
	public class OpenBookIDNetMessage : ClientMessage<OpenBookIDNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong BookID;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestSendBook(msg.BookID, SentByPlayer.GameObject);
		}

		public static NetMessage Send(ulong bookId)
		{
			NetMessage msg = new NetMessage
			{
				BookID = bookId
			};

			Send(msg);
			return msg;
		}
	}
}
