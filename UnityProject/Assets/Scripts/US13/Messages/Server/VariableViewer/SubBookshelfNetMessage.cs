using Mirror;
using SecureStuff;
using UnityEngine;
using US13.UI.Systems;
using US13.Variable_Viewer;
using Util;

namespace US13.Messages.Server.VariableViewer
{
	public class SubBookshelfNetMessage : ServerMessage<SubBookshelfNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;
			public bool Teleport;
			public ClientObjectPath.PathData PathData;

		}

		public override void Process(NetMessage msg)
		{
			var NetworkedObject = ClientObjectPath.GetObjectMessage(msg.PathData);
			UIManager.Instance.UI_BooksInBookshelf.ValueSetUp(msg.BookShelf, NetworkedObject, msg.Teleport);
			UIManager.Instance.VariableViewer.Open();

		}

		public static NetMessage Send(Librarian.Library.LibraryBookShelf _BookShelf, GameObject ToWho, bool RequestTeleport)
		{
			NetMessage msg = new NetMessage()
			{
				BookShelf = VariableViewerNetworking.ProcessSubBookShelf(_BookShelf),
				Teleport =  RequestTeleport

			};
			msg.PathData = ClientObjectPath.GetPathForMessage(_BookShelf.Shelf);
			SendTo(ToWho, msg, channel : 3);
			return msg;
		}
	}
}
