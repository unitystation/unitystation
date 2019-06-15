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

	public class NetFriendlySentence
	{
		public uint SentenceID;
		public uint PagePosition;
		public string KeyVariable;
		public string KeyVariableType;

		public string ValueVariable;
		public string ValueVariableType;

		public ulong OnPageID;
		public List<NetFriendlySentence> Sentences;

	}

	public class NetFriendlyPage
	{
		public ulong ID;
		public string VariableName;
		public string Variable;
		public string VariableType;

		public string Sentences;

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}
		//public Book BindedTo; unneeded?
	}

	public class NetFriendlyBook
	{
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


	public static short MessageType = (short)MessageTypes.BookNetMessage;
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
		string Classe;
		if (_book.IsnotMono)
		{
			Classe = _book.NonMonoBookClass.ToString();
		}
		else {
			Classe = _book.BookClass.GetType().Name;
		}
		BookNetMessage msg = new BookNetMessage
		{
			Book = new NetFriendlyBook()
			{
				ID = _book.ID,
				//BindedPages = _book.BindedPages.ToArray(),
				IsEnabled = _book.IsEnabled,
				BookClassname = Classe,
				Title = _book.Title,
				UnGenerated = _book.UnGenerated
			}
		};
		List<NetFriendlyPage> ListPages = new List<NetFriendlyPage>();
		foreach (var bob in _book.BindedPages)
		{
			NetFriendlyPage Page = new NetFriendlyPage
			{
				ID = bob.ID,
				Variable = bob.Variable.ToString(),
				VariableName = bob.VariableName,
				VariableType = bob.VariableType.ToString()
			};
			if (bob.Sentences.Sentences != null && (bob.Sentences.Sentences.Count > 0))
			{
				NetFriendlySentence NetFriendlySentences = new NetFriendlySentence();
				RecursiveSentencePopulate(NetFriendlySentences, bob.Sentences);
				Page.Sentences = JsonConvert.SerializeObject(NetFriendlySentences);
				Logger.Log(Page.Sentences);
			}
			ListPages.Add(Page);
		}

		msg.Book.BindedPages = ListPages.ToArray();
		msg.SendToAll();
		return msg;
	}

	public static void RecursiveSentencePopulate(NetFriendlySentence NetFriendlySentence, Librarian.Sentence LibrarianSentence) {
		if (LibrarianSentence.Sentences != null)
		{
			NetFriendlySentence.Sentences = new List<NetFriendlySentence>();
			foreach (var _Sentence in LibrarianSentence.Sentences)
			{
				NetFriendlySentence FriendlySentence = new NetFriendlySentence()
				{
					PagePosition = _Sentence.PagePosition,
					SentenceID = _Sentence.SentenceID,
					ValueVariable = _Sentence.ValueVariable.ToString(),
					ValueVariableType = _Sentence.ValueVariableType.ToString(),
					OnPageID = _Sentence.OnPageID,
				};
				if (_Sentence.KeyVariable != null)
				{
					FriendlySentence.KeyVariable = _Sentence.KeyVariable.ToString();
					FriendlySentence.KeyVariableType = _Sentence.KeyVariableType.ToString();
				}
				if (_Sentence.Sentences != null)
				{
					RecursiveSentencePopulate(FriendlySentence, _Sentence);
				}

				NetFriendlySentence.Sentences.Add(FriendlySentence);
			}
		}
	}
}
