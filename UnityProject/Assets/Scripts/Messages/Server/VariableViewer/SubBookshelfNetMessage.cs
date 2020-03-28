using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Bson;
using System;
public class SubBookshelfNetMessage : ServerMessage
{
   	public override short MessageType => (short)MessageTypes.SubBookshelfNetMessage;
	public string data;
	public VariableViewerNetworking.NetFriendlyBookShelf BookShelf;

	public override IEnumerator Process()
	{

		UIManager.Instance.BookshelfViewer.BookShelfIn = BookShelf;
		//UIManager.Instance.BookshelfViewer.BookShelfIn = JsonConvert.DeserializeObject<VariableViewerNetworking.NetFriendlyBookShelf>(data);
		return null;
	}

	public static SubBookshelfNetMessage Send(Librarian.BookShelf _BookShelf)
	{
		SubBookshelfNetMessage msg = new SubBookshelfNetMessage()
		{
			BookShelf = VariableViewerNetworking.ProcessSUBBookShelf(_BookShelf)
		};
		//VariableViewerNetworking.NetFriendlyBookShelf bookshedl = ;
		//Logger.Log(bookshedl.OBS.Length.ToString() + " << YY");
		//msg.BookShelf = bookshedl;
		//msg.data = JsonConvert.SerializeObject(bookshedl);

		//Logger.Log(msg.data);
		msg.SendToAll();
		return msg;
	}

}
