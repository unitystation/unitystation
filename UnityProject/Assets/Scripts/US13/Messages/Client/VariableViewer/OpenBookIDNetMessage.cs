using Mirror;
using US13.Messages.Client.Admin;

namespace US13.Messages.Client.VariableViewer
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
			if (HasPermission(TAG.VARIABLE_VIEWER) == false) return;

			global::US13.Variable_Viewer.BookViewer.VariableViewer.RequestSendBook(msg.BookID, SentByPlayer.GameObject);
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
