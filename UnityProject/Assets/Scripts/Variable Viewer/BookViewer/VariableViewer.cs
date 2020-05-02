using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using System.Text;
using System.Linq;
using Component = UnityEngine.Component;
using Object = System.Object;


// TODO
// Colour code
// pool books

public static class VariableViewer
{
	public static void ProcessTile(Vector3 Location, GameObject WhoBy)
	{
		Location.z = 0f;
		Vector3Int worldPosInt = Location.To2Int().To3Int();
		Matrix matrix = MatrixManager.AtPoint(worldPosInt, true).Matrix;

		Location = matrix.transform.InverseTransformPoint(Location);
		Vector3Int tilePosition = Vector3Int.FloorToInt(Location);

		var registerTiles = matrix.Get<RegisterTile>(tilePosition, false);

		List<GameObject> _Objects = registerTiles.Select(x => x.gameObject).ToList();

		//include interactable tiles
		var interactableTiles = matrix.GetComponentInParent<InteractableTiles>();
		if (interactableTiles != null)
		{
			_Objects.Add(interactableTiles.gameObject);
		}

		List<Transform> transforms = new List<Transform>();
		foreach (var Object in _Objects)
		{
			transforms.Add(Object.transform);
		}

		ProcessListOnTileTransform(transforms, WhoBy);
	}

	public static void ProcessListOnTileTransform(List<Transform> transform, GameObject WhoBy)
	{
		List<Librarian.BookShelf> BookShelfs = new List<Librarian.BookShelf>();
		for (int i = 0; i < transform.Count; i++)
		{
			if (Librarian.TransformToBookShelf.ContainsKey(transform[i]))
			{
				BookShelfs.Add(Librarian.TransformToBookShelf[transform[i]]);
			}
			else
			{
				BookShelfs.Add(Librarian.BookShelf.PartialGeneratebookShelf(transform[i]));
			}
		}

		SendBookShelfToClient(Librarian.GenerateCustomBookCase(BookShelfs), WhoBy);
	}

	public static Librarian.BookShelf ProcessTransform(Transform transform, GameObject ByWho)
	{
		Librarian.BookShelf BookShelf;
		if (Librarian.TransformToBookShelf.ContainsKey(transform))
		{
			BookShelf = Librarian.TransformToBookShelf[transform];
		}
		else
		{
			BookShelf = Librarian.BookShelf.PartialGeneratebookShelf(transform);
		}

		//BookShelf.PopulateBookShelf();
		SendBookShelfToClient(Librarian.GenerateCustomBookCase(BookShelf),ByWho);

		return (BookShelf);
	}

	public static void ProcessOpenBook(ulong BookID, GameObject ByWho) //yes yes if you Have high Ping then rip, -Creator
	{
		Librarian.Book Book;
		if (Librarian.IDToBook.ContainsKey(BookID))
		{
			Book = Librarian.IDToBook[BookID];
			if (Book.UnGenerated)
			{
				Book = Librarian.Book.PopulateBook(Book);
			}

			SendBookToClient(Book,ByWho);
		}
		else
		{
			Logger.LogError("book ID has not been generated  BookID > " + BookID, Category.VariableViewer);
		}
	}

	public static void SendBookToClient(Librarian.Book Book, GameObject ToWho)
	{
		if (!Book.UnGenerated)
		{
			foreach (var page in Book.BindedPages)
			{
				page.UpdatePage();
			}
		}
		else
		{
			Book.GetBindedPages();
		}

		BookNetMessage.Send(Book,ToWho);
	}

	public static void SendBookShelfToClient(Librarian.BookShelf BookShelf, GameObject ToWho)
	{
		BookShelf.UpdateBookShelf();
		BookshelfNetMessage.Send(BookShelf, ToWho);
	}

	//Receive from Client side
	public static void RequestOpenPageValue(ulong PageID, uint SentenceID, bool IsSentence, bool iskey, GameObject ToWho)
	{
		//if ( client authorised )
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			Librarian.Page Page = Librarian.IDToPage[PageID];
			Librarian.Book book;
			if (!IsSentence)
			{
				MonoBehaviour _MonoBehaviour = Page.Variable as MonoBehaviour;
				if (_MonoBehaviour == null)
				{
					if ((Page.Variable as string) == "null")
					{
						Logger.LogWarning("Trying to process page value as book PageID > " + PageID,
							Category.VariableViewer);
						return;
					}

					book = Librarian.Book.GenerateNonMonoBook(Page.Variable);
					SendBookToClient(book,ToWho);
				}
				else
				{
					book = Librarian.Book.PartialGeneratebook(_MonoBehaviour);
					book = Librarian.Book.PopulateBook(book);
					SendBookToClient(book,ToWho);
				}
			}
			else
			{
				if (iskey)
				{
					book = Librarian.Book.GenerateNonMonoBook(Page.IDtoSentence[SentenceID].KeyVariable);
					SendBookToClient(book, ToWho);
				}
				else
				{
					book = Librarian.Book.GenerateNonMonoBook(Page.IDtoSentence[SentenceID].ValueVariable);
					SendBookToClient(book, ToWho);
				}
			}
		}
		else
		{
			Logger.LogError("Page ID has not been generated PageID > " + PageID, Category.VariableViewer);
		}
	}

	public static void RequestSendBookshelf(ulong BookshelfID, bool IsNewbookBookshelf, GameObject WhoBy)
	{
		if (Librarian.IDToBookShelf.ContainsKey(BookshelfID))
		{
			Librarian.BookShelf Bookshelf = Librarian.IDToBookShelf[BookshelfID];
			if (IsNewbookBookshelf)
			{
				if (!Bookshelf.ICustomBookshelf && Bookshelf.Shelf == null)
				{
					Logger.LogError("Bookshelf has been destroyed > " + BookshelfID, Category.VariableViewer);
					return;
				}

				if (Bookshelf.IsPartiallyGenerated)
				{
					Bookshelf.PopulateBookShelf();
				}

				if (Bookshelf.ObscuredBy != null)
				{
					SendBookShelfToClient(Bookshelf.ObscuredBy,WhoBy);
				}
			}
			else
			{
				SubBookshelfNetMessage.Send(Bookshelf);
			}
		}
		else
		{
			Logger.LogError("Bookshelf ID has not been generated BookshelfID > " + BookshelfID,
				Category.VariableViewer);
		}
	}

	public static void RequestSendBook(ulong BookID, GameObject ByWho)
	{
		//if ( client authorised )
		if (Librarian.IDToBook.ContainsKey(BookID))
		{
			SendBookToClient(Librarian.IDToBook[BookID], ByWho);
		}
		else
		{
			Logger.LogError("Book ID has not been generated Book ID > " + BookID, Category.VariableViewer);
		}
	}


	public static void RequestChangeVariable(ulong PageID, string ChangeTo, bool SendToClient, GameObject WhoBy, string AdminId)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				WhoBy.name + " Modified " + Librarian.IDToPage[PageID].VariableName + " on " +  Librarian.IDToPage[PageID].BindedTo.Title
				+ " From " + VVUIElementHandler.Serialise(Librarian.IDToPage[PageID].Variable, Librarian.IDToPage[PageID].VariableType) + " to "+ ChangeTo
				+ " with Send to clients? " + SendToClient, AdminId);
			Librarian.PageSetValue(Librarian.IDToPage[PageID], ChangeTo);
			if (SendToClient)
			{
				var monoBehaviour = (Librarian.IDToPage[PageID].BindedTo.BookClass as Component);
				UpdateClientValue.Send(ChangeTo, Librarian.IDToPage[PageID].VariableName,
					TypeDescriptor.GetClassName(monoBehaviour),
					monoBehaviour.gameObject);
			}
		}
		else
		{
			Logger.LogError("Page ID has not been generated Page ID > " + PageID, Category.VariableViewer);
		}
	}
	//public static void RequestChangeSentenceVariable(ulong PageID, uint SentenceID, string ChangeTo)
	//{
	//	if (Librarian.IDToPage.ContainsKey(PageID))
	//	{
	//		if (Librarian.IDToPage[PageID].IDtoSentence.ContainsKey(SentenceID)) {
	//			Librarian.PageSetValue(Librarian.IDToPage[PageID], ChangeTo);
	//		}
	//		else
	//		{
	//			Logger.LogError("Sentence ID has not been generated in Page Page ID > " + PageID + " Sentence ID >  " + SentenceID, Category.VariableViewer);
	//		}

	//	}
	//	else
	//	{
	//		Logger.LogError("Page ID has not been generated Page ID > " + PageID, Category.VariableViewer);
	//	}
	//}
}

public static class Librarian
{
	public static Dictionary<ulong, BookShelf> IDToBookShelf = new Dictionary<ulong, BookShelf>();
	public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
	public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();

	public static ulong BookShelfAID = 1;
	public static ulong BookAID = 1;
	public static ulong PageAID = 1;

	public static Dictionary<Transform, BookShelf> TransformToBookShelf = new Dictionary<Transform, BookShelf>();
	public static Dictionary<MonoBehaviour, Book> MonoBehaviourToBook = new Dictionary<MonoBehaviour, Book>();
	public static Dictionary<object, Book> ObjectToBook = new Dictionary<object, Book>();

	public static BookShelf TopSceneBookshelf;

	public static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");

	public static void Reset()
	{
		TopSceneBookshelf = null;
		ObjectToBook.Clear();
		MonoBehaviourToBook.Clear();
		TransformToBookShelf.Clear();
		BookShelfAID = 1;
		BookAID = 1;
		PageAID = 1;

		IDToPage.Clear();
		IDToBook.Clear();
		IDToBookShelf.Clear();
	}


	public static void PageSetValue(Page Page, string newValue)
	{
		Page.SetValue(newValue);
	}


	public static BookShelf GenerateCustomBookCase(List<BookShelf> BookShelfs)
	{
		BookShelf Customshelf = new BookShelf();
		Customshelf.ID = BookShelfAID;
		BookShelfAID++;
		Customshelf.IsPartiallyGenerated = false;
		Customshelf.ShelfName = "Your custom list of bookshelves";
		Customshelf.ObscuredBookShelves = BookShelfs;
		Customshelf.ICustomBookshelf = true;
		IDToBookShelf[Customshelf.ID] = Customshelf;
		return (Customshelf);
	}

	public static BookShelf GenerateCustomBookCase(BookShelf BookShelf)
	{
		BookShelf Customshelf = new BookShelf();
		Customshelf.ID = BookShelfAID;
		BookShelfAID++;
		Customshelf.IsPartiallyGenerated = false;
		Customshelf.ShelfName = "Your custom list of bookshelves";
		Customshelf.ObscuredBookShelves.Add(BookShelf);
		Customshelf.ICustomBookshelf = true;
		IDToBookShelf[Customshelf.ID] = Customshelf;
		return (Customshelf);
	}

	public static Book GetAttributes(Book Book, object Script)
	{
		Type monoType = Script.GetType();
		var Fields = monoType.BaseType.GetFields(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
			BindingFlags.FlattenHierarchy
		);

		var coolFields = Fields.ToList();

		coolFields.AddRange((monoType.GetFields(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
			BindingFlags.FlattenHierarchy
		).ToList()));

		foreach (FieldInfo Field in coolFields)
		{
			if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = Field.Name;
				Page.ID = PageAID;
				PageAID++;
				Page.Info = Field;
				Page.Variable = Field.GetValue(Script);
				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}

				Page.VariableType = Field.FieldType;
				Page.BindedTo = Book;
				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;
				GenerateSentenceValuesforSentence(Page.Sentences, Field.FieldType, Page, Script, Info: Field);
				Book.BindedPagesAdd(Page);
			}
		}

		//.BaseType
		if (TupleTypeReference != monoType
		) //Causes an error if this is not here and Tuples can not get Custom properties so it is I needed to get the properties
		{
			var Propertie = monoType.BaseType.GetProperties(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			);

			var coolProperties = Propertie.ToList();

			coolProperties.AddRange((monoType.GetProperties(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			).ToList()));
			foreach (PropertyInfo Properties in coolProperties)
			{
				if (Properties.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					Page Page = new Page();
					Page.VariableName = Properties.Name;
					Page.Variable = Properties.GetValue(Script);
					Page.VariableType = Properties.PropertyType;
					Page.PInfo = Properties;
					if (Page.Variable == null)
					{
						Page.Variable = "null";
					}

					Page.ID = PageAID;
					PageAID++;
					Page.BindedTo = Book;
					IDToPage[Page.ID] = Page;
					Page.Sentences = new Librarian.Sentence();
					Page.Sentences.SentenceID = Page.ASentenceID;
					Page.ASentenceID++;
					GenerateSentenceValuesforSentence(Page.Sentences, Properties.PropertyType, Page, Script,
						PInfo: Properties);
					Book.BindedPagesAdd(Page);
				}
			}
		}

		return (Book);
	}

	public static void GenerateSentenceValuesforSentence(Sentence sentence, Type VariableType, Page Page, object Script,
		FieldInfo Info = null, PropertyInfo PInfo = null)
	{
		if (Info == null && PInfo == null)
		{
			foreach (FieldInfo method in VariableType.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			))
			{
				if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (method.FieldType.IsGenericType)
					{
						IEnumerable list = method.GetValue(Script) as IEnumerable;
						if (sentence.Sentences == null)
						{
							sentence.Sentences = new List<Sentence>();
						}

						uint count = 0;
						if (list != null)
						{
							foreach (var c in list)
							{
								Sentence _sentence = new Sentence();
								_sentence.ValueVariable = c;
								_sentence.OnPageID = Page.ID;
								_sentence.PagePosition = count;
								_sentence.ValueVariableType = c.GetType();
								_sentence.SentenceID = Page.ASentenceID;
								Page.ASentenceID++;
								Page.IDtoSentence[_sentence.SentenceID] = _sentence;
								Type valueType = c.GetType();
								if (valueType.IsGenericType)
								{
									Type baseType = valueType.GetGenericTypeDefinition();
									if (baseType == typeof(KeyValuePair<,>))
									{
										_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
										_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);

										_sentence.ValueVariableType = valueType.GetGenericArguments()[1];
										_sentence.KeyVariableType = valueType.GetGenericArguments()[0];
									}
								}

								if (!valueType.IsClass)
								{
									GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
								}

								count++;
								sentence.Sentences.Add(_sentence);
							}
						}
					}
				}
			}
		}
		else
		{
			if (VariableType.IsGenericType)
			{
				IEnumerable list;
				Type TType;
				if (Info == null)
				{
					list = PInfo.GetValue(Script) as IEnumerable; //icollection<keyvaluepair>
					TType = PInfo.PropertyType;
				}
				else
				{
					list = Info.GetValue(Script) as IEnumerable; //
					TType = Info.FieldType;
				}

				sentence.Sentences = new List<Sentence>();
				uint count = 0;
				if (list != null)
				{
					foreach (object c in list)
					{
						Sentence _sentence = new Sentence();
						_sentence.ValueVariable = c;
						_sentence.OnPageID = Page.ID;
						_sentence.PagePosition = count;
						_sentence.ValueVariableType = c.GetType();
						_sentence.SentenceID = Page.ASentenceID;
						Page.ASentenceID++;
						Page.IDtoSentence[_sentence.SentenceID] = _sentence;

						Type valueType = c.GetType();
						if (valueType.IsGenericType)
						{
							Type baseType = valueType.GetGenericTypeDefinition();
							if (baseType == typeof(KeyValuePair<,>))
							{
								_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
								_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);

								_sentence.ValueVariableType = valueType.GetGenericArguments()[1];
								_sentence.KeyVariableType = valueType.GetGenericArguments()[0];
							}
						}

						if (!valueType.IsClass)
						{
							GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
						}

						count++;
						Page.Sentences.Sentences.Add(_sentence);
					}
				}
			}
		}
	}


	public class BookShelf
	{
		public ulong ID;
		public string ShelfName;
		public bool IsEnabled;

		public GameObject Shelf;

		//public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();
		public bool IsPartiallyGenerated = true;
		public bool ICustomBookshelf = false;
		public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();

		public BookShelf ObscuredBy;
		public List<Book> HeldBooks = new List<Book>();

		public void UpdateBookShelf(bool firstLayer = true)
		{
			if (IsPartiallyGenerated)
			{
				PopulateBookShelf();
				return;
			}

			if (firstLayer)
			{
				List<BookShelf> toremove = new List<BookShelf>();
				foreach (BookShelf _bookshelf in ObscuredBookShelves)
				{
					if ((!_bookshelf.ICustomBookshelf))
					{
						if (_bookshelf.Shelf != null)
						{
							_bookshelf.UpdateBookShelf(false);
						}
						else
						{
							toremove.Add(_bookshelf);
							//poolbook?
						}
					}
				}

				foreach (BookShelf _bookshelf in toremove)
				{
					ObscuredBookShelves.Remove(_bookshelf);
				}
			}

			if (!ICustomBookshelf)
			{
				MonoBehaviour[] scriptComponents = Shelf.GetComponents<MonoBehaviour>();
				HeldBooks.Clear();
				foreach (MonoBehaviour mono in scriptComponents)
				{
					if (mono != null)
					{
						HeldBooks.Add(Book.PartialGeneratebook(mono));
					}
				}

				ObscuredBookShelves.Clear();
				Transform[] ts = Shelf.GetComponentsInChildren<Transform>();
				foreach (Transform child in ts)
				{
					if (child != ts[0])
					{
						if (child.parent == Shelf.transform)
						{
							BookShelf _bookShelf;
							_bookShelf = PartialGeneratebookShelf(child);
							ObscuredBookShelves.Add(_bookShelf);
						}
					}
				}

				Transform _Transform = Shelf.transform.parent;
				if (_Transform != null)
				{
					ObscuredBy = PartialGeneratebookShelf(_Transform);
				}
				else
				{
					if (TopSceneBookshelf == null)
					{
						List<BookShelf> BookShelfs = new List<BookShelf>();
						foreach (var game in UnityEngine.SceneManagement.SceneManager.GetActiveScene()
							.GetRootGameObjects())
						{
							var _bookShelf = PartialGeneratebookShelf(game.transform);
							BookShelfs.Add(_bookShelf);
						}

						TopSceneBookshelf = GenerateCustomBookCase(BookShelfs);
					}

					ObscuredBy = TopSceneBookshelf;
					if (!TopSceneBookshelf.ObscuredBookShelves.Contains(this))
					{
						TopSceneBookshelf.ObscuredBookShelves.Add(this);
					}
				}

				IsPartiallyGenerated = false;
			}
		}

		public void PopulateBookShelf()
		{
			if (!this.IsPartiallyGenerated)
			{
				return;
			}

			MonoBehaviour[] scriptComponents = this.Shelf.GetComponents<MonoBehaviour>();
			//Logger.Log(scriptComponents.Length + "leit !!!");

			foreach (MonoBehaviour mono in scriptComponents)
			{
				if (mono != null)
				{
					this.HeldBooks.Add(Book.PartialGeneratebook(mono));
				}
			}

			Transform[] ts = this.Shelf.GetComponentsInChildren<Transform>();
			foreach (Transform child in ts)
			{
				if (child != ts[0])
				{
					if (child.parent == this.Shelf.transform)
					{
						BookShelf _bookShelf;
						_bookShelf = PartialGeneratebookShelf(child);
						this.ObscuredBookShelves.Add(_bookShelf);
					}
				}
			}

			Transform _Transform = this.Shelf.transform.parent;

			if (_Transform != null)
			{
				this.ObscuredBy = PartialGeneratebookShelf(_Transform);
			}
			else
			{
				if (TopSceneBookshelf == null)
				{
					List<BookShelf> BookShelfs = new List<BookShelf>();
					foreach (var game in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
					{
						var _bookShelf = PartialGeneratebookShelf(game.transform);
						BookShelfs.Add(_bookShelf);
					}

					TopSceneBookshelf = GenerateCustomBookCase(BookShelfs);
				}

				this.ObscuredBy = TopSceneBookshelf;
				if (!TopSceneBookshelf.ObscuredBookShelves.Contains(this))
				{
					TopSceneBookshelf.ObscuredBookShelves.Add(this);
				}
			}

			this.IsPartiallyGenerated = false;
			return;
		}

		public static BookShelf PartialGeneratebookShelf(Transform _Transform)
		{
			if (TransformToBookShelf.ContainsKey(_Transform))
			{
				return (TransformToBookShelf[_Transform]);
			}

			BookShelf _bookShelf = new BookShelf();
			_bookShelf.ShelfName = _Transform.gameObject.name;
			_bookShelf.ID = BookShelfAID;
			BookShelfAID++;
			_bookShelf.Shelf = _Transform.gameObject;
			if (_bookShelf.Shelf == null)
			{
				Logger.LogError("HELP");
			}

			IDToBookShelf[_bookShelf.ID] = _bookShelf;
			TransformToBookShelf[_bookShelf.Shelf.transform] = _bookShelf;
			return (_bookShelf);
		}

		public override string ToString()
		{
			StringBuilder logMessage = new StringBuilder();
			logMessage.AppendLine("ShelfName > " + ShelfName);
			logMessage.Append("Books > \n");
			logMessage.AppendLine(string.Join("\n", HeldBooks));
			logMessage.Append("ObscuredBookShelves  > \n");
			logMessage.AppendLine(string.Join("\n", ObscuredBookShelves));

			return (logMessage.ToString());
		}
	}

	public class Book
	{
		public ulong ID;
		public string Title;
		public object BookClass;
		public bool IsnotMono;

		private List<Page> _BindedPages = new List<Page>();

		public List<Page> BindedPages
		{
			get
			{
				if (UnGenerated)
				{
					Logger.LogWarning("USE GetBindedPages()!,since these books are ungenerated ");
				}

				return _BindedPages;
			}
			set { _BindedPages = value; }
		}

		public bool UnGenerated = true;

		public List<Page> GetBindedPages()
		{
			if (UnGenerated)
			{
				if (BookClass != null)
				{
					PopulateBook(this);
				}
				else
				{
					Logger.LogError("Book has been destroyed!" + ID);
				}

				UnGenerated = false;
			}

			return (_BindedPages);
		}

		public void BindedPagesAdd(Page page)
		{
			_BindedPages.Add(page);
		}


		public override string ToString()
		{
			StringBuilder logMessage = new StringBuilder();
			logMessage.AppendLine("Title > " + Title);
			logMessage.Append("Pages > ");
			logMessage.AppendLine(string.Join("\n", BindedPages));

			return (logMessage.ToString());
		}

		public static Book PopulateBook(Book book)
		{
			if (!book.UnGenerated)
			{
				return (book);
			}

			var mono = book.BookClass;
			book.UnGenerated = false;
			Type monoType = mono.GetType();
			book = GetAttributes(book, mono);
			return (book);
		}


		public static Book PartialGeneratebook(MonoBehaviour mono)
		{
			if (MonoBehaviourToBook.ContainsKey(mono))
			{
				return (MonoBehaviourToBook[mono]);
			}

			Book book = new Book();
			book.ID = BookAID;
			BookAID++;
			book.BookClass = mono;
			book.Title = mono.ToString();
			IDToBook[book.ID] = book;
			MonoBehaviourToBook[mono] = book;
			return (book);
		}

		public static Book GenerateNonMonoBook(object Eclass)
		{
			if (ObjectToBook.ContainsKey(Eclass))
			{
				return (ObjectToBook[Eclass]);
			}

			Type TType = Eclass.GetType();
			Book book = new Book();
			book.ID = BookAID;
			BookAID++;
			book.BookClass = Eclass;
			book.IsnotMono = true;
			book.UnGenerated = false;
			book.Title = Eclass.ToString();
			ObjectToBook[Eclass] = book;
			IDToBook[book.ID] = book;

			book = GetAttributes(book, Eclass);
			return (book);
		}
	}


	public class Page
	{
		public ulong ID;
		public string VariableName;
		public object Variable;
		public Type VariableType;
		public string AssemblyQualifiedName;
		public Book BindedTo;
		public PropertyInfo PInfo;
		public FieldInfo Info;

		public uint ASentenceID;
		public Sentence Sentences;

		public Dictionary<uint, Sentence> IDtoSentence = new Dictionary<uint, Sentence>();

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}

		public void SetValue(string Value)
		{
			//Logger.Log(this.ToString());
			//Logger.Log(ID.ToString());
			//Logger.Log(Variable.GetType().ToString());
			if (PInfo != null)
			{
				PInfo.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
				//prop.SetValue(service, Enum.Parse(prop.PropertyType, "Item1"), null);
			}
			else if (Info != null)
			{
				Info.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
			}

			UpdatePage();
		}

		public static object DeSerialiseValue(object InObject, string StringVariable, Type InType)
		{
			if (VVUIElementHandler.Type2Element.ContainsKey(InType))
			{
				return (VVUIElementHandler.Type2Element[InType].DeSerialise(StringVariable));
			}
			else
			{
				if (InType == typeof(Enum))
				{
					//if ()
					return Enum.Parse(InObject.GetType(), StringVariable);
				}
				else
				{
					return Convert.ChangeType(StringVariable, InObject.GetType());
				}
			}
		}


		public void UpdatePage()
		{
			if (PInfo != null)
			{
				Variable = PInfo.GetValue(BindedTo.BookClass);
			}
			else
			{
				Variable = Info.GetValue(BindedTo.BookClass);
			}

			if (Variable == null)
			{
				Variable = "null";
			}

			//GenerateSentenceValuesforSentence
			if (Sentences.Sentences != null)
			{
				IDtoSentence.Clear();
				Sentences = new Sentence();
				ASentenceID = 0;
				Sentences.SentenceID = ASentenceID;
				ASentenceID++;
				if (PInfo != null)
				{
					GenerateSentenceValuesforSentence(Sentences, PInfo.PropertyType, this, BindedTo.BookClass,
						PInfo: PInfo);
				}
				else
				{
					GenerateSentenceValuesforSentence(Sentences, Info.FieldType, this, BindedTo.BookClass, Info: Info);
				}
			}
		}
	}

	public class Sentence
	{
		public uint SentenceID;
		public uint PagePosition;
		public object KeyVariable;
		public Type KeyVariableType;

		public object ValueVariable;
		public Type ValueVariableType;

		public List<Sentence> Sentences;
		public ulong OnPageID;
	}


	public static Type UEGetType(string TypeName)
	{
		if (TypeName == null || TypeName == "")
		{
			return null;
		}

		// Try Type.GetType() first. This will work with types defined
		// by the Mono runtime, in the same assembly as the caller, etc.
		var type = Type.GetType(TypeName);

		// If it worked, then we're done here
		if (type != null)
			return type;

		// If the TypeName is a full name, then we can try loading the defining assembly directly
		if (TypeName.Contains("."))
		{
			// Get the name of the assembly (Assumption is that we are using
			// fully-qualified type names)
			var assemblyName = TypeName.Substring(0, TypeName.IndexOf('.'));

			// Attempt to load the indicated Assembly
			var assembly = Assembly.Load(assemblyName);
			if (assembly == null)
				return null;

			// Ask that assembly to return the proper Type
			type = assembly.GetType(TypeName);
			if (type != null)
				return type;
		}

		// If we still haven't found the proper type, we can enumerate all of the
		// loaded assemblies and see if any of them define the type
		var currentAssembly = Assembly.GetExecutingAssembly();
		var referencedAssemblies = currentAssembly.GetReferencedAssemblies();
		foreach (var assemblyName in referencedAssemblies)
		{
			// Load the referenced assembly
			var assembly = Assembly.Load(assemblyName);
			if (assembly != null)
			{
				// See if that assembly defines the named type
				type = assembly.GetType(TypeName);
				if (type != null)
					return type;
			}
		}

		// The type just couldn't be found...
		return null;
	}
}

public enum DisplayValueType
{
	Bools,
	Ints,
	Floats,
	Strings,
	Classes,
}