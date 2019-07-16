using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestBookshelfNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestBookshelfNetMessage;
	public ulong BookshelfID;
	public bool IsNewBookshelf = false;

	public override IEnumerator Process()
	{
		VariableViewer.RequestSendBookshelf(BookshelfID, IsNewBookshelf);
		yield return null;
	}


	public static RequestBookshelfNetMessage Send(ulong _BookshelfID, bool _IsNewBookshelf = false)
	{
		RequestBookshelfNetMessage msg = new RequestBookshelfNetMessage();
		msg.BookshelfID = _BookshelfID;
		msg.IsNewBookshelf = _IsNewBookshelf;

		msg.Send();
		return msg;
	}
}
