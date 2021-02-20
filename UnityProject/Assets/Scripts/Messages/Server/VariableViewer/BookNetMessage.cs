using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class BookNetMessage : ServerMessage
{
	public class BookNetMessageNetMessage : ActualMessage
	{
		public VariableViewerNetworking.NetFriendlyBook Book;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as BookNetMessageNetMessage;
		if(newMsg == null) return;

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
