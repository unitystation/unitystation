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
	public struct SubBookshelfNetMessageNetMessage : NetworkMessage
	{
		public string data;
		public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public SubBookshelfNetMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as SubBookshelfNetMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
