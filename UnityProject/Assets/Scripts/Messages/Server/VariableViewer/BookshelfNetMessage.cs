using Mirror;
using UnityEngine;

namespace Messages.Server.VariableViewer
{
	public class BookshelfNetMessage : ServerMessage<BookshelfNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public VariableViewerNetworking.NetFriendlyBookShelfView data;
		}

		public override void Process(NetMessage msg)
		{
			//JsonConvert.DeserializeObject<VariableViewerNetworking.NetFriendlyBookShelfView>()
			//Logger.Log(JsonConvert.SerializeObject(data));
			UIManager.Instance.BookshelfViewer.BookShelfView = msg.data;
			UIManager.Instance.BookshelfViewer.ValueSetUp();
		}

		public static NetMessage Send(Librarian.BookShelf _BookShelf, GameObject ToWho)
		{
			NetMessage msg = new NetMessage();
			msg.data = VariableViewerNetworking.ProcessBookShelf(_BookShelf);

			SendTo(ToWho, msg);
			return msg;
		}
	}
}