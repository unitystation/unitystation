using Logs;
using Mirror;
using SecureStuff;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class BookNetMessage : ServerMessage<BookNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public VariableViewerNetworking.NetFriendlyBook Book;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.VariableViewer.ReceiveBook(msg.Book);
		}

		public static NetMessage Send(Librarian.Book _book, GameObject ToWho)
		{
			NetMessage msg = new NetMessage
			{
				Book = VariableViewerNetworking.ProcessBook(_book)
			};

			SendTo(ToWho, msg, Category.VariableViewer, 3);
			return msg;
		}
	}
}
