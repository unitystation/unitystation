using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;

public class BookNetMessage : ServerMessage
{
	public class NetFriendlyPage
	{
		public ulong ID;
		public string VariableName;
		public string Variable;
		public string VariableType;

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}
		//public Book BindedTo; unneeded?
	}

	public class NetFriendlyBook { 
		public ulong ID;
		public string Title;
		public string BookClassname;
		public bool IsEnabled;
		public NetFriendlyPage[] BindedPages;
		public bool UnGenerated = true;

		public override string ToString()
		{
			StringBuilder logMessage = new StringBuilder();
			logMessage.AppendLine("Title > " + Title);
			logMessage.Append("Pages > ");
			//logMessage.AppendLine(string.Join("\n", BindedPages));

			return (logMessage.ToString());
		}
	}


	public static short MessageType = (short) MessageTypes.BookNetMessage;
	public NetFriendlyBook Book;

	public override IEnumerator Process()
	{

		UIManager.Instance.VariableViewer.ID = Book.ID;
		UIManager.Instance.VariableViewer.Title = Book.Title;
		UIManager.Instance.VariableViewer.IsEnabled = Book.IsEnabled;
		UIManager.Instance.VariableViewer.ReceiveBook(Book);
		return null;
	}

	public static BookNetMessage Send(Librarian.Book _book)
	{
		BookNetMessage msg = new BookNetMessage
		{
			Book = new NetFriendlyBook()
			{
				ID = _book.ID,
				//BindedPages = _book.BindedPages.ToArray(),
				IsEnabled = _book.IsEnabled,
				BookClassname = _book.BookClass.GetType().Name,
				Title = _book.Title,
				UnGenerated = _book.UnGenerated
			}
		};
		List<NetFriendlyPage> ListPages = new List<NetFriendlyPage>();
		foreach (var bob in _book.BindedPages) {
			NetFriendlyPage Page = new NetFriendlyPage
			{
				ID = bob.ID,
				Variable = bob.Variable.ToString(),
				VariableName = bob.VariableName,
				VariableType = bob.VariableType.ToString()
			};
			ListPages.Add(Page);
		}

		msg.Book.BindedPages = ListPages.ToArray();
		msg.SendToAll();
		return msg;
	}
}
