using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Bson;
using System;
public class SubBookshelfNetMessage : ServerMessage
{
	public string data;
	public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;

	public override void Process()
	{
		UIManager.Instance.BookshelfViewer.BookShelfIn = BookShelf;
	}

	public static SubBookshelfNetMessage Send(Librarian.BookShelf _BookShelf)
	{
		SubBookshelfNetMessage msg = new SubBookshelfNetMessage()
		{
			BookShelf = VariableViewerNetworking.ProcessSUBBookShelf(_BookShelf)
		};
		msg.SendToAll();
		return msg;
	}
}
