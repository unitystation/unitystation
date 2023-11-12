using Mirror;
using SecureStuff;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class SubBookshelfNetMessage : ServerMessage<SubBookshelfNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.UI_BooksInBookshelf.ValueSetUp(msg.BookShelf);
		}

		public static NetMessage Send(Librarian.Library.LibraryBookShelf _BookShelf, GameObject ToWho)
		{
			NetMessage msg = new NetMessage()
			{
				BookShelf = VariableViewerNetworking.ProcessSubBookShelf(_BookShelf)
			};

			SendTo(ToWho, msg, channel : 3);
			return msg;
		}
	}
}
