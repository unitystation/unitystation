using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;

public class VariableViewerNetworking : MonoBehaviour
{
	public class IDnName {
		public ulong ID;
		/// <summary>
		/// The name of the shelf.
		/// ShelfName
		/// </summary>
		public string SN;
	}

	public class NetFriendlyBookShelfView
	{
		public ulong ID;
		public IDnName[] HeldShelfIDs;

	}

	public class NetFriendlyBookShelf
	{
		public ulong ID;

		/// <summary>
		/// ShelfName The name of the shelf.
		/// </summary>
		public string SN;

		/// <summary>
		/// IsEnabled is enabled.
		/// </summary>
		public bool IE;

		/// <summary>
		/// ObscuredBookShelves
		/// </summary>
		public IDnName[] OBS;

		/// <summary>
		/// ObscuredBy
		/// </summary>
		public IDnName OB;

		/// <summary>
		/// HeldBooks
		/// </summary>
		public IDnName[] HB;
	}

	public class NetFriendlySentence
	{

		public uint SentenceID;

		public uint PagePosition;

		public string KeyVariable;

		public string KeyVariableType;

		public string ValueVariable;

		public string ValueVariableType;

		public ulong OnPageID;


		public uint HeldBySentenceID;


		private List<NetFriendlySentence> Sentences;

		public void SetSentences(List<NetFriendlySentence> _Sentences)
		{
			Sentences = _Sentences;
		}

		public List<NetFriendlySentence> GetSentences()
		{
			return (Sentences);
		}

	}

	public class NetFriendlyPage
	{
		public ulong ID;
		public string VariableName;
		public string Variable;
		public string VariableType;

		public NetFriendlySentence[] Sentences;

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}

		public void ProcessSentences()
		{
			Dictionary<uint, NetFriendlySentence> DictionaryStore = new Dictionary<uint, NetFriendlySentence>();
			if (Sentences.Length > 0)
			{
				//Logger.Log("YOOOOO");
				NetFriendlySentence TOPClientFriendlySentence = Sentences[0];
				DictionaryStore[Sentences[0].SentenceID] = TOPClientFriendlySentence;
				//Logger.LogError(JsonConvert.SerializeObject(Sentences));
				foreach (var bob in Sentences)
				{
					bob.SetSentences(new List<NetFriendlySentence>());
					DictionaryStore[bob.SentenceID] = bob;
				}
				foreach (var bob in Sentences)
				{
					if (bob.SentenceID != 0)
					{
						if (DictionaryStore.ContainsKey(bob.HeldBySentenceID))
						{
							//Logger.LogError("added" + bob.ValueVariable);
							DictionaryStore[bob.HeldBySentenceID].GetSentences().Add(bob);
						}
					}

				}
				NetFriendlySentence[] _bob = new NetFriendlySentence[1] {
				TOPClientFriendlySentence
				};
				Sentences = _bob;
				//Logger.Log("TT > " + JsonConvert.SerializeObject(Sentences));
				//Logger.Log(JsonConvert.SerializeObject(Sentences[0].GetSentences()));
			}
		}


	}

	public class NetFriendlyBook
	{
		public ulong ID;
		public string Title;
		public string BookClassname;
		//public bool IsEnabled;
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

	public static IDnName ProcessBookShelfToID(Librarian.BookShelf _BookShelf)
	{
		IDnName _IDnName = new IDnName
		{
			ID = _BookShelf.ID,
			SN = _BookShelf.ShelfName,
		};
		if (_IDnName.SN == null) {
			_IDnName.SN = "null";
		}
		return (_IDnName);
	}

	public static NetFriendlyBookShelf ProcessSUBBookShelf(Librarian.BookShelf _BookShelf)  {
		if (_BookShelf.IsPartiallyGenerated) {
			_BookShelf.PopulateBookShelf();
		}

		NetFriendlyBookShelf BookShelf = new NetFriendlyBookShelf
		{
			ID = _BookShelf.ID,
			SN = _BookShelf.ShelfName,
			IE = _BookShelf.IsEnabled,
		};
		List<IDnName> lisofIDnName = new List<IDnName>();
		foreach (var ObscuredBookShelve in _BookShelf.ObscuredBookShelves) {
			IDnName _IDnName = new IDnName
			{
				ID = ObscuredBookShelve.ID,
				SN = ObscuredBookShelve.ShelfName
			};
			if (_IDnName.SN == null)
			{_IDnName.SN = "null";}
			lisofIDnName.Add(_IDnName);
		}
		BookShelf.OBS = lisofIDnName.ToArray();
		List<IDnName> _lisofIDnName = new List<IDnName>();
		foreach (var HeldBook in _BookShelf.HeldBooks)
		{
			IDnName _IDnName = new IDnName
			{
				ID = HeldBook.ID,
				SN = HeldBook.BookClass.GetType().Name
			};
			if (_IDnName.SN == null)
			{ _IDnName.SN = "null"; }
			_lisofIDnName.Add(_IDnName);
		}


		BookShelf.HB = _lisofIDnName.ToArray();


		BookShelf.OB = new IDnName
		{ID = _BookShelf.ObscuredBy.ID,
		SN = _BookShelf.ObscuredBy.ShelfName,};
		return (BookShelf);
	}

	public static NetFriendlyBookShelfView ProcessBookShelf(Librarian.BookShelf _BookShelf)  {
		NetFriendlyBookShelfView TopBookShelf = new NetFriendlyBookShelfView
		{
			ID = _BookShelf.ID
		};
		List<IDnName> NetFriendlyBookShelfs = new List<IDnName>();
		if (_BookShelf.IsPartiallyGenerated) {
			_BookShelf.PopulateBookShelf();
		}
		foreach (var ObscuredBookShelve in _BookShelf.ObscuredBookShelves) {
			NetFriendlyBookShelfs.Add(ProcessBookShelfToID(ObscuredBookShelve));
		}
		TopBookShelf.HeldShelfIDs = NetFriendlyBookShelfs.ToArray();

		return (TopBookShelf);
	}

	public static NetFriendlyBook ProcessBook(Librarian.Book _book) {
		string Classe;
		Classe = _book.BookClass.GetType().Name;
		NetFriendlyBook Book = new NetFriendlyBook()
		{
			ID = _book.ID,
			BookClassname = Classe,
			Title = _book.Title,
			UnGenerated = _book.UnGenerated
		};

		List<NetFriendlyPage> ListPages = new List<NetFriendlyPage>();
		foreach (var bob in _book.GetBindedPages())
		{
			List<NetFriendlySentence> Sentences = new List<NetFriendlySentence>();
			NetFriendlyPage Page = new NetFriendlyPage
			{
				ID = bob.ID,
				Variable = VVUIElementHandler.Serialise(bob.Variable, bob.VariableType),
				VariableName = bob.VariableName,
				VariableType = bob.VariableType.ToString()
			};
			if (Librarian.UEGetType(Page.VariableType) == null)
			{
				Page.VariableType = bob.VariableType.AssemblyQualifiedName;
			}
			if (bob.Sentences.Sentences != null && (bob.Sentences.Sentences.Count > 0))
			{
				NetFriendlySentence FriendlySentence = new NetFriendlySentence()
				{
					HeldBySentenceID = bob.Sentences.SentenceID
				};
				FriendlySentence.OnPageID = 8888888;
				Sentences.Add(FriendlySentence);
				RecursiveSentencePopulate(bob.Sentences, Sentences);
			}
			Page.Sentences = Sentences.ToArray();
			ListPages.Add(Page);
		}
		Book.BindedPages = ListPages.ToArray();

		return (Book);

	}

	public static void RecursiveSentencePopulate(Librarian.Sentence LibrarianSentence, List<NetFriendlySentence> Sentences)
	{
		if (LibrarianSentence.Sentences != null)
		{
			foreach (var _Sentence in LibrarianSentence.Sentences)
			{
				NetFriendlySentence FriendlySentence = new NetFriendlySentence()
				{
					PagePosition = _Sentence.PagePosition,
					SentenceID = _Sentence.SentenceID,

					ValueVariable = VVUIElementHandler.Serialise(_Sentence.ValueVariable, _Sentence.ValueVariableType),
					ValueVariableType = _Sentence.ValueVariableType.ToString(),
					OnPageID = _Sentence.OnPageID,
					HeldBySentenceID = LibrarianSentence.SentenceID
				};
				if (Librarian.UEGetType(FriendlySentence.ValueVariableType) == null)
				{
					FriendlySentence.ValueVariableType = _Sentence.ValueVariableType.AssemblyQualifiedName;
				}
				if (_Sentence.KeyVariable != null)
				{
					FriendlySentence.KeyVariable =
						VVUIElementHandler.Serialise(_Sentence.KeyVariable, _Sentence.KeyVariableType);
					FriendlySentence.KeyVariableType = _Sentence.KeyVariableType.ToString();
					if ((FriendlySentence.KeyVariableType == null) || (Librarian.UEGetType(FriendlySentence.KeyVariableType) == null))
					{
						FriendlySentence.KeyVariableType = _Sentence.KeyVariableType.AssemblyQualifiedName;
					}
				}


				if (_Sentence.Sentences != null)
				{
					RecursiveSentencePopulate( _Sentence,Sentences);
				}

				Sentences.Add(FriendlySentence);
			}
		}
	}
}
