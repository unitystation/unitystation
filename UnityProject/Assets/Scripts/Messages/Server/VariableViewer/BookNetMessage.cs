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

public class BookNetMessage : ServerMessage
{
	public class BookNetMessageNetMessage : NetworkMessage
	{
		public VariableViewerNetworking.NetFriendlyBook Book;
	}

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
