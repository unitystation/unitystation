using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Logs;
using Newtonsoft.Json;
using SecureStuff;

public class VariableViewerNetworking : MonoBehaviour
{
	public class IDnName
	{
		public ulong ID;

		/// <summary>
		/// The name of the shelf.
		/// ShelfName
		/// </summary>
		public string SN;
	}

	public class NetFriendlyHierarchyBookShelf
	{
		public string Nm;
		public ulong ID;
		public ulong PID;

		private NetFriendlyHierarchyBookShelf Parent;

		public void SetParent(NetFriendlyHierarchyBookShelf _Parent)
		{
			Parent = _Parent;
		}


		public NetFriendlyHierarchyBookShelf GetParent()
		{
			return Parent;
		}

		private List<NetFriendlyHierarchyBookShelf>  Children = new List<NetFriendlyHierarchyBookShelf>();


		public List<NetFriendlyHierarchyBookShelf> GetChildrenList()
		{
			return Children;
		}


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

		/// <summary>
		/// Get size of this object (in bytes)
		/// </summary>
		/// <returns>size of this object (in bytes)</returns>
		public int GetSize()
		{
			// !	IMPORTANT	!
			// remember to change this method content after modyfing data structure
			return sizeof(uint)                             // SentenceID
				+ sizeof(uint)                              // PagePosition
				+ sizeof(char) * KeyVariable.Length         // KeyVariable
				+ sizeof(char) * KeyVariableType.Length     // KeyVariableType
				+ sizeof(char) * ValueVariable.Length       // ValueVariable
				+ sizeof(char) * ValueVariableType.Length   // ValueVariableType
				+ sizeof(ulong)                             // HeldBySentenceID
				+ (Sentences == null ? 0 : Sentences.Sum(x => x.GetSize()));          // Size of all sentences
		}

	}

	public class NetFriendlyPage
	{
		public ulong ID;
		public string VariableName;
		public string Variable;
		public string VariableType;
		public string FullVariableType;
		public bool CanWrite = true;
		public VVHighlight VVHighlight = VVHighlight.None;

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

		/// <summary>
		/// Get size of this object (in bytes)
		/// </summary>
		/// <returns>size of this object (in bytes)</returns>
		public int GetSize()
		{
			// !	IMPORTANT	!
			// remember to change this method content after modyfing data structure
			return sizeof(ulong)                        // ID
				+ sizeof(char) * VariableName.Length    // VariableName
				+ sizeof(char) * Variable.Length        // Variable
				+ sizeof(char) * VariableType.Length    // VariableType
				+ (Sentences == null ? 0 : Sentences.Sum(x => x.GetSize()));		// Size of all sentences
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

		/// <summary>
		/// Get size of this object (in bytes)
		/// </summary>
		/// <returns>size of this object (in bytes)</returns>
		public int GetSize()
		{
			//TODO!!!
			// !	IMPORTANT	!
			// remember to change this method content after modyfing data structure
			return sizeof(ulong)
				+ sizeof(char) * Title.Length
				+ sizeof(char) * BookClassname.Length
				+ (BindedPages == null ? 0 : BindedPages.Sum(x => x.GetSize()))
				+ sizeof(bool);
		}
	}

	public static NetFriendlyBookShelf ProcessSubBookShelf(Librarian.Library.LibraryBookShelf _BookShelf)  {
		if (_BookShelf.IsPartiallyGenerated) {
			_BookShelf.PopulateBookShelf();
		}

		NetFriendlyBookShelf BookShelf = new NetFriendlyBookShelf
		{
			ID = _BookShelf.ID,
			SN = _BookShelf.ShelfName,
			IE = _BookShelf.IsEnabled,
		};
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


		return (BookShelf);
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

		// get max possible packet size from current transform
		int maxPacketSize = Mirror.Transport.active.GetMaxPacketSize(0);
		// set currentSize start value to max TCP header size (60b)
		int currentSize = 60;

		List<NetFriendlyPage> ListPages = new List<NetFriendlyPage>();
		foreach (var bob in _book.GetBindedPages())
		{
			List<NetFriendlySentence> Sentences = new List<NetFriendlySentence>();
			NetFriendlyPage Page = new NetFriendlyPage
			{
				ID = bob.ID,
				Variable = VVUIElementHandler.Serialise(bob.Variable, bob.VariableType),
				VariableName = bob.VariableName,
				VariableType = bob.VariableType?.ToString(),
				VVHighlight = bob.VVHighlight,
				FullVariableType = bob.AssemblyQualifiedName
			};
			if (bob.PInfo != null)
			{
				Page.CanWrite = bob.PCanWrite;
			}



			if (Librarian.UEGetType(Page.FullVariableType) == null)
			{
				Page.VariableType = bob?.VariableType?.AssemblyQualifiedName;
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

			//currentSize += Page.GetSize(); //TODO work out why this causes errors


			// if currentSize is greater than the maxPacketSize - break loop and send message
			if (currentSize > maxPacketSize)
			{
				Loggy.LogError("[VariableViewerNetworking.ProcessBook] - message is to big to send in one packet", Category.VariableViewer);
				break;
			}

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

	public static List<NetFriendlyHierarchyBookShelf> ProcessLibrary( Librarian.Library Library)
	{

		List<NetFriendlyHierarchyBookShelf>  Toreturn = new List<NetFriendlyHierarchyBookShelf>();
		foreach (var roots in Library.Roots)
		{
			Toreturn.Add(ProcessLibraryBookShelf(Librarian.TransformToBookShelf[roots.Shelf.transform]));
			RecursiveProcessLibrary(roots, Toreturn);
		}

		return Toreturn;
	}


	public static void RecursiveProcessLibrary(Librarian.Library.LibraryBookShelf LibraryBookShelf,
		List<NetFriendlyHierarchyBookShelf> List)
	{
		foreach (var Children in LibraryBookShelf.Contains)
		{
			List.Add(ProcessLibraryBookShelf(Librarian.TransformToBookShelf[Children])); //################
			RecursiveProcessLibrary(Librarian.TransformToBookShelf[Children], List);
		}
	}

	public static NetFriendlyHierarchyBookShelf ProcessLibraryBookShelf(Librarian.Library.LibraryBookShelf LibraryBookShelf)
	{
		var newone = new NetFriendlyHierarchyBookShelf();
		newone.Nm = LibraryBookShelf.ShelfName;
		if (LibraryBookShelf.Parent != null)
		{
			newone.PID = Librarian.TransformToBookShelf[LibraryBookShelf.Parent].ID;
		}

		newone.ID = LibraryBookShelf.ID;
		return newone;
	}

}
