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

	public override short MessageType => (short)MessageTypes.BookNetMessage;
	public VariableViewerNetworking.NetFriendlyBook Book;

	public override IEnumerator Process()
	{
		UIManager.Instance.VariableViewer.ReceiveBook(Book);
		return null;
	}

	public static BookNetMessage Send(Librarian.Book _book)
	{
		BookNetMessage msg = new BookNetMessage
		{
			Book = VariableViewerNetworking.ProcessBook(_book)
		};
		msg.SendToAll();
		return msg;
	}


}
