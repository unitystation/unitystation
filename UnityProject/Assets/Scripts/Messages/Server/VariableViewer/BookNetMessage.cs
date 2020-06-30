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
	public VariableViewerNetworking.NetFriendlyBook Book;

	public override void Process()
	{
		UIManager.Instance.VariableViewer.ReceiveBook(Book);
	}

	public static BookNetMessage Send(Librarian.Book _book, GameObject ToWho)
	{
		BookNetMessage msg = new BookNetMessage
		{
			Book = VariableViewerNetworking.ProcessBook(_book)
		};
		msg.SendTo(ToWho);
		return msg;
	}


}
