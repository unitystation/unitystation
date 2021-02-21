using UnityEngine;
using Mirror;

public class BookNetMessage : ServerMessage
{
	public struct BookNetMessageNetMessage : NetworkMessage
	{
		public VariableViewerNetworking.NetFriendlyBook Book;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public BookNetMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as BookNetMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		UIManager.Instance.VariableViewer.ReceiveBook(newMsg.Book);
	}

	public static BookNetMessageNetMessage Send(Librarian.Book _book, GameObject ToWho)
	{
		BookNetMessageNetMessage msg = new BookNetMessageNetMessage
		{
			Book = VariableViewerNetworking.ProcessBook(_book)
		};
		new BookNetMessage().SendTo(ToWho, msg);
		return msg;
	}
}
