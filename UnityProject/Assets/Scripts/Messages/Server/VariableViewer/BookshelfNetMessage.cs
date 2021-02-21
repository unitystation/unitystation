using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Mirror;
using Newtonsoft.Json;


public class BookshelfNetMessage : ServerMessage
{
	public class BookshelfNetMessageNetMessage : NetworkMessage
	{
		public VariableViewerNetworking.NetFriendlyBookShelfView data;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as BookshelfNetMessageNetMessage;
		if(newMsg == null) return;

		//JsonConvert.DeserializeObject<VariableViewerNetworking.NetFriendlyBookShelfView>()
		//Logger.Log(JsonConvert.SerializeObject(data));
		UIManager.Instance.BookshelfViewer.BookShelfView = newMsg.data;
		UIManager.Instance.BookshelfViewer.ValueSetUp();
	}

	public static BookshelfNetMessageNetMessage Send(Librarian.BookShelf _BookShelf, GameObject ToWho)
	{
		BookshelfNetMessageNetMessage msg = new BookshelfNetMessageNetMessage();
		msg.data = VariableViewerNetworking.ProcessBookShelf(_BookShelf);

		new BookshelfNetMessage().SendTo(ToWho, msg);
		return msg;
	}
}