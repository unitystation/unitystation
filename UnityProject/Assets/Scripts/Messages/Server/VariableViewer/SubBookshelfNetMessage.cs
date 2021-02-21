using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Bson;
using System;
using Mirror;

public class SubBookshelfNetMessage : ServerMessage
{
	public class SubBookshelfNetMessageNetMessage : NetworkMessage
	{
		public string data;
		public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as SubBookshelfNetMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.BookshelfViewer.BookShelfIn = newMsg.BookShelf;
	}

	public static SubBookshelfNetMessageNetMessage Send(Librarian.BookShelf _BookShelf)
	{
		SubBookshelfNetMessageNetMessage msg = new SubBookshelfNetMessageNetMessage()
		{
			BookShelf = VariableViewerNetworking.ProcessSUBBookShelf(_BookShelf)
		};
		new SubBookshelfNetMessage().SendToAll(msg);
		return msg;
	}
}
