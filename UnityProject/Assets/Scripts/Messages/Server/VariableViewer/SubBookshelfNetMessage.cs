using Mirror;

namespace Messages.Server.VariableViewer
{
	public class SubBookshelfNetMessage : ServerMessage<SubBookshelfNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string data;
			public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.BookshelfViewer.BookShelfIn = msg.BookShelf;
		}

		public static NetMessage Send(Librarian.BookShelf _BookShelf)
		{
			NetMessage msg = new NetMessage()
			{
				BookShelf = VariableViewerNetworking.ProcessSUBBookShelf(_BookShelf)
			};

			SendToAll(msg);
			return msg;
		}
	}
}
