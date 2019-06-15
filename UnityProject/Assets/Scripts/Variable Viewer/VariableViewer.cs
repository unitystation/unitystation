using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;


// TODO 
// pooling
// Preexisting ID checks for Books
// Colour code

public static class VariableViewer
{

	public static void ProcessListOnTileTransform(List<Transform> transform)
	{
		List<Librarian.BookShelf> BookShelfs = new List<Librarian.BookShelf>();
		for (int i = 0; i < transform.Count; i++)
		{

			if (Librarian.TransformToBookShelf.ContainsKey(transform[i]))
			{
				BookShelfs.Add(Librarian.TransformToBookShelf[transform[i]]);
			}
			else {
				BookShelfs.Add(Librarian.PartialGeneratebookShelf(transform[i]));
			}
		}
		SendBookShelfToClient(Librarian.GenerateCustomBookCase(BookShelfs));
	}

	public static Librarian.BookShelf ProcessTransform(Transform transform)
	{
		Librarian.BookShelf BookShelf;
		if (Librarian.TransformToBookShelf.ContainsKey(transform))
		{
			BookShelf = Librarian.TransformToBookShelf[transform];
		}
		else {
			BookShelf = Librarian.PartialGeneratebookShelf(transform);
		}
		SendBookShelfToClient(BookShelf);
		return (BookShelf);
	}

	public static void ProcessOpenBook(ulong BookID) //yes yes if you Have high Ping then rip, -Creator
	{
		Librarian.Book Book;
		if (Librarian.IDToBook.ContainsKey(BookID))
		{
			Book = Librarian.IDToBook[BookID];
			if (Book.UnGenerated)
			{
				Book = Librarian.PopulateBook(Book);
			}
			SendBookToClient(Book);
		}
		else {
			Logger.LogError("book ID has not been generated  BookID > " + BookID);
		}

	}

	public static void SendBookToClient(Librarian.Book Book)
	{
		BookNetMessage.Send(Book);
	}

	public static void SendBookShelfToClient(Librarian.BookShelf BookShelf)
	{
		//Send BookShelf
	}



	public static void PrintSomeVariables(GameObject _object)
	{

		var bob = ProcessTransform(_object.transform);

		var booke = bob.GetHeldBooks();
		Logger.Log(booke.Count.ToString()); //(booke.Count - 1)
		var bookeE = booke[3];
		bookeE.GetBindedPages();
		BookNetMessage.Send(bookeE);
		//For each monoBehaviour in the list of script components
		//foreach (MonoBehaviour mono in scriptComponents)
		//{

		//Librarian.Book book = Librarian.GenerateBook(mono);
		//Logger.Log(book.ToString());
		//Type monoType = mono.GetType();
		//foreach (MethodInfo method in monoType.GetMethods()) // BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
		//{
		//	Logger.Log(method.Name + " < this ");
		//}
		//Logger.LogWarning(" method " + monoType.Name);
		//	foreach (PropertyInfo method in monoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) // 
		//	{
		//		if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
		//		{
		//			Logger.Log(method.Name + " " + method.GetValue(mono));
		//		}
		//	}
		//	foreach (FieldInfo method in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) // 
		//	{
		//		if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
		//		{
		//			Logger.LogWarning(method.FieldType + " < FieldType! ");
		//			Logger.Log(method.Name + " " + method.GetValue(mono));
		//		}
		//	}
		//}
	}
	//Receive from Client side
	public static void RequestOpenPageValue(ulong PageID)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			Librarian.Page Page = Librarian.IDToPage[PageID];
			Librarian.Book book;
			MonoBehaviour _MonoBehaviour = Page.Variable as MonoBehaviour;
			if (_MonoBehaviour == null)
			{
				if ((Page.Variable as string) == "null")
				{					Logger.LogWarning("Trying to process page value as book PageID > " + PageID);
					return;
				}
				book = Librarian.GenerateNonMonoBook(Page.Variable); //Currently dangerous needs type to book implemented for it
				BookNetMessage.Send(book);
			}
			else {
				book = Librarian.PartialGeneratebook(_MonoBehaviour); //Currently dangerous needs MonoBehaviour to book implemented for it
				book = Librarian.PopulateBook(book);
				BookNetMessage.Send(book);
			}
		}
		else {
			Logger.LogError("Page ID has not been generated PageID > " + PageID);
		}
	}


	public static void RequestSendList_Dict(ulong PageID)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			Librarian.Page Page = Librarian.IDToPage[PageID];
			//Page.FindSentences();
		}
		else {
			Logger.LogError("Page ID has not been generated PageID > " + PageID);
		}
	}

}

public static class Librarian
{

	public static Dictionary<ulong, BookShelf> IDToBookShelf = new Dictionary<ulong, BookShelf>();
	public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
	public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();


	public static ulong BookShelfAID = 0;
	public static ulong BookAID = 0;
	public static ulong PageAID = 0;

	public static Dictionary<Transform, BookShelf> TransformToBookShelf = new Dictionary<Transform, BookShelf>();
	public static Dictionary<MonoBehaviour, Book> MonoBehaviourToBook = new Dictionary<MonoBehaviour, Book>();
	public static Dictionary<object, Book> ObjectToBook = new Dictionary<object, Book>();

	//public static Dictionary<Client, BookShelf> Customshelfs = new Dictionary<Client, BookShelf>
	public static BookShelf Customshelf;

	//public static ulong AvailableBookShelfID= 0;
	//public static ulong AvailableBookID = 0;
	//public static ulong AvailablePageID = 0;

	public static BookShelf GenerateCustomBookCase(List<BookShelf> BookShelfs)
	{
		if (Customshelf != null)
		{
			Customshelf.ObscuredBookShelves = BookShelfs;
			return (Customshelf);
		}
		else
		{
			Customshelf = new BookShelf();
			Customshelf.ID = BookShelfAID;
			BookShelfAID++;
			Customshelf.ShelfName = "Your custom list of bookshelves";
			Customshelf.ObscuredBookShelves = BookShelfs;
			IDToBookShelf[Customshelf.ID] = Customshelf;
			return (Customshelf);
		}
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
		book.IsEnabled = mono.isActiveAndEnabled;
		IDToBook[book.ID] = book;
		MonoBehaviourToBook[mono] = book;
		return (book);
	}

	public static Book GenerateNonMonoBook(object Eclass)
	{ //Currently dangerous needs type to book implemented for it

		if (ObjectToBook.ContainsKey(Eclass))
		{
			return (ObjectToBook[Eclass]);
		}
		Type TType = Eclass.GetType();
		Book book = new Book();
		book.ID = BookAID;
		BookAID++;
		book.NonMonoBookClass = TType;
		book.IsnotMono = true;
		book.UnGenerated = false;
		book.Title = Eclass.ToString();
		ObjectToBook[Eclass] = book;
		IDToBook[book.ID] = book;

		foreach (FieldInfo method in TType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.ID = PageAID;
				PageAID++;
				Page.Variable = method.GetValue(Eclass);
				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}
				Page.VariableType = method.FieldType;
				Page.BindedTo = book;

				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				GenerateSentenceValuesforSentence(Page.Sentences, method.FieldType, Page, method, Info: method);
				book.BindedPagesAdd(Page);
			}
		}

		foreach (PropertyInfo method in TType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.Variable = method.GetValue(Eclass);
				Page.VariableType = method.PropertyType;
				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}
				Page.ID = PageAID;
				PageAID++;
				Page.BindedTo = book;
				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				GenerateSentenceValuesforSentence(Page.Sentences, method.PropertyType, Page, method, PInfo: method);
				book.BindedPagesAdd(Page);
			}
		}
		return (book);
	}

	public static BookShelf PartialGeneratebookShelf(Transform _Transform)
	{
		BookShelf _bookShelf = new BookShelf();
		_bookShelf.ShelfName = _Transform.gameObject.name;
		_bookShelf.ID = BookShelfAID;
		BookShelfAID++;
		_bookShelf.Shelf = _Transform.gameObject;

		IDToBookShelf[_bookShelf.ID] = _bookShelf;
		TransformToBookShelf[_bookShelf.Shelf.transform] = _bookShelf;
		return (_bookShelf);
	}

	public static BookShelf PopulateBookShelf(BookShelf bookShelf)
	{

		MonoBehaviour[] scriptComponents = bookShelf.Shelf.GetComponents<MonoBehaviour>();
		Logger.Log(scriptComponents.Length + "leit !!!");
		foreach (MonoBehaviour mono in scriptComponents)
		{
			bookShelf.HeldBooksAdd(PartialGeneratebook(mono));
		}
		Transform[] ts = bookShelf.Shelf.GetComponentsInChildren<Transform>();
		foreach (Transform child in ts)
		{
			if (child != ts[0])
			{
				BookShelf _bookShelf = new BookShelf();
				if (TransformToBookShelf.ContainsKey(child))
				{
					_bookShelf = TransformToBookShelf[child];
				}
				else {
					_bookShelf = PartialGeneratebookShelf(child);
				}

				bookShelf.ObscuredBookShelves.Add(_bookShelf);
			}
		}
		Transform _Transform = bookShelf.Shelf.transform.parent;
		if (TransformToBookShelf.ContainsKey(_Transform))
		{
			bookShelf.ObscuredBy = TransformToBookShelf[_Transform];
		}
		else
		{
			bookShelf.ObscuredBy = PartialGeneratebookShelf(_Transform);
		}

		return (bookShelf);
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
		foreach (FieldInfo method in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.ID = PageAID;
				PageAID++;
				Page.Variable = method.GetValue(mono);
				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}
				Page.VariableType = method.FieldType;
				Page.BindedTo = book;
				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				GenerateSentenceValuesforSentence(Page.Sentences, method.FieldType, Page, mono, Info: method);
				book.BindedPagesAdd(Page);
			}
		}

		foreach (PropertyInfo method in monoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.Variable = method.GetValue(mono);
				Page.VariableType = method.PropertyType;
				if (Page.Variable == null)
				{
					Page.Variable = "null";
				}
				Page.ID = PageAID;
				PageAID++;
				Page.BindedTo = book;
				IDToPage[Page.ID] = Page;
				Page.Sentences = new Librarian.Sentence();
				GenerateSentenceValuesforSentence(Page.Sentences, method.PropertyType, Page, mono, PInfo : method );

				book.BindedPagesAdd(Page);
			}
		}
		return (book);
	}


	public static void GenerateSentenceValuesforSentence(Sentence sentence, Type VariableType, Page Page, object Script, FieldInfo Info = null, PropertyInfo PInfo = null)
	{
		if (Info == null && PInfo == null)
		{
			foreach (FieldInfo method in VariableType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static))
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
									Type[] argTypes = baseType.GetGenericArguments();
									sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
									sentence.KeyVariableType = valueType.GetProperty("Key").GetType();
									sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);
									sentence.ValueVariableType = valueType.GetProperty("Value").GetType();

								}
							}
							GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
							count++;
							sentence.Sentences.Add(_sentence);
						}
					}
				}
			}
		}
		else { 
			if (VariableType.IsGenericType)
			{
				IEnumerable list;
				if (Info == null)
				{
					list = PInfo.GetValue(Script) as IEnumerable; //icollection<keyvaluepair>

				}
				else
				{
					list = Info.GetValue(Script) as IEnumerable; //

				}
		
				sentence.Sentences = new List<Sentence>();
				uint count = 0;
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
							Type[] argTypes = baseType.GetGenericArguments();
							_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
							_sentence.KeyVariableType = valueType.GetProperty("Key").GetType();
							_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);
							_sentence.ValueVariableType = valueType.GetProperty("Value").GetType();

						}
					}
					GenerateSentenceValuesforSentence(_sentence, c.GetType(), Page, c);
					count++;
					Page.Sentences.Sentences.Add(_sentence);
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
		public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();
		public BookShelf ObscuredBy;

		private List<Book> _HeldBooks = new List<Book>();
		public List<Book> HeldBooks
		{
			get
			{
				if (HeldBooksUnGenerated)
				{
					Logger.LogWarning("USE GetHeldBooks()!,since these books are ungenerated ");
				}
				return _HeldBooks;
			}
			set { _HeldBooks = value; }
		}
		bool HeldBooksUnGenerated = true;

		public List<Book> GetHeldBooks()
		{
			if (HeldBooksUnGenerated)
			{
				PopulateBookShelf(this);
				HeldBooksUnGenerated = false;
			}
			return (_HeldBooks);
		}

		public void HeldBooksAdd(Book book)
		{
			_HeldBooks.Add(book);
		}

		public override string ToString()
		{
			StringBuilder logMessage = new StringBuilder();
			logMessage.AppendLine("ShelfName > " + ShelfName);
			logMessage.Append("Books > \n");
			logMessage.AppendLine(string.Join("\n", _HeldBooks));
			logMessage.Append("ObscuredBookShelves  > \n");
			logMessage.AppendLine(string.Join("\n", ObscuredBookShelves));

			return (logMessage.ToString());
		}
	}

	[Serializable]
	public class Book
	{
		public ulong ID;
		public string Title;
		public MonoBehaviour BookClass;
		public Type NonMonoBookClass;
		public bool IsnotMono;
		public bool IsEnabled;

		private List<Page> _BindedPages = new List<Page>();
		public List<Page> BindedPages
		{
			get
			{
				if (UnGenerated)
				{
					Logger.LogWarning("USE GetHeldBooks()!,since these books are ungenerated ");
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
				PopulateBook(this);
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
	}


	public class Page
	{
		public ulong ID;
		public string VariableName;
		public object Variable;
		public Type VariableType;
		public Book BindedTo;

		public uint ASentenceID;
		public Sentence Sentences;

		public Dictionary<uint, Sentence> IDtoSentence = new Dictionary<uint, Sentence>();

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
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
