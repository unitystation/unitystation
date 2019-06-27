using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;


// TODO 
// Code duplication galore.
// also about 10 FieldInfos named method that hurts.
// Colour code

public static class VariableViewer
{
	// objects selectable for cloning
	public static LayerMask layerMask;

	public static void ProcessTile(Vector3 Location)
	{
		//var _Objects = MouseUtils.GetOrderedObjectsAtPoint(Location, layerMask);
		Location.z = 0f;
		List<GameObject> _Objects = UITileList.GetItemsAtPosition(Location);
		List<Transform> transforms = new List<Transform>();
		foreach (var Object in _Objects) {
			Logger.Log("RR");
			transforms.Add(Object.transform);
		}
		Logger.Log(transforms.Count + "YYOOLL");
		ProcessListOnTileTransform(transforms);
	}

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
		BookshelfNetMessage.Send(BookShelf);
	}



	public static void PrintSomeVariables(GameObject _object)
	{

		var bob = ProcessTransform(_object.transform);
		Librarian.PopulateBookShelf(bob);
		//Librarian.PopulateBookShelf(bob.ObscuredBy);
		var booke = bob.HeldBooks;
		var bookeE = booke[3];
		bookeE.GetBindedPages();
		BookshelfNetMessage.Send(bob.ObscuredBy);
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


	public static void RequestSendBookshelf(ulong BookshelfID, bool IsNewbookBookshelf)
	{
		if (Librarian.IDToBookShelf.ContainsKey(BookshelfID))
		{
			Librarian.BookShelf Bookshelf = Librarian.IDToBookShelf[BookshelfID];
			if (IsNewbookBookshelf) {
				Logger.Log("man");
				if (Bookshelf.IsPartiallyGenerated) {
					Logger.Log("man2");
					Librarian.PopulateBookShelf(Bookshelf);
				}
				Logger.Log("Bookshelf.ObscuredBy != null");
				if (Bookshelf.ObscuredBy != null)
				{
					Logger.Log("man3 != null");
					BookshelfNetMessage.Send(Bookshelf.ObscuredBy);
				}
			
			} 
			else { SubBookshelfNetMessage.Send(Bookshelf); }


		}
		else {
			Logger.LogError("Bookshelf ID has not been generated BookshelfID > " + BookshelfID);
		}
	}

	public static void RequestSendBook(ulong BookID)
	{
		if (Librarian.IDToBook.ContainsKey(BookID))
		{
			Librarian.Book Book = Librarian.IDToBook[BookID];
			BookNetMessage.Send(Book);
		}
		else {
			Logger.LogError("Book ID has not been generated Book ID > " + BookID);
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


	public static BookShelf TopSceneBookshelf;
	//public static ulong AvailableBookShelfID= 0;
	//public static ulong AvailableBookID = 0;
	//public static ulong AvailablePageID = 0;

	public static BookShelf GenerateCustomBookCase(List<BookShelf> BookShelfs)
	{
		BookShelf Customshelf = new BookShelf();
		Customshelf.ID = BookShelfAID;
		BookShelfAID++;
		Customshelf.IsPartiallyGenerated = false;
		Customshelf.ShelfName = "Your custom list of bookshelves";
		Customshelf.ObscuredBookShelves = BookShelfs;
		IDToBookShelf[Customshelf.ID] = Customshelf;
		return (Customshelf);

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
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;
				GenerateSentenceValuesforSentence(Page.Sentences, method.FieldType, Page, Eclass, Info: method);
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
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;
				GenerateSentenceValuesforSentence(Page.Sentences, method.PropertyType, Page, Eclass, PInfo: method);
				book.BindedPagesAdd(Page);
			}
		}
		return (book);
	}

	public static BookShelf PartialGeneratebookShelf(Transform _Transform)
	{
		if (TransformToBookShelf.ContainsKey(_Transform)) {
			return (TransformToBookShelf[_Transform]);
		}
		BookShelf _bookShelf = new BookShelf();
		_bookShelf.ShelfName = _Transform.gameObject.name;
		_bookShelf.ID = BookShelfAID;
		BookShelfAID++;
		_bookShelf.Shelf = _Transform.gameObject;
		if (_bookShelf.Shelf == null) {
			Logger.LogError("HELP");
		}
		IDToBookShelf[_bookShelf.ID] = _bookShelf;
		TransformToBookShelf[_bookShelf.Shelf.transform] = _bookShelf;
		return (_bookShelf);
	}

	public static BookShelf PopulateBookShelf(BookShelf bookShelf)
	{
		if (!bookShelf.IsPartiallyGenerated) {
			return (bookShelf);
		}
		MonoBehaviour[] scriptComponents = bookShelf.Shelf.GetComponents<MonoBehaviour>();
		//Logger.Log(scriptComponents.Length + "leit !!!");
		foreach (MonoBehaviour mono in scriptComponents)
		{
			bookShelf.HeldBooks.Add(PartialGeneratebook(mono));
		}
		Transform[] ts = bookShelf.Shelf.GetComponentsInChildren<Transform>();
		foreach (Transform child in ts)
		{
			if (child != ts[0])
			{
				if (child.parent == bookShelf.Shelf.transform)
				{
					BookShelf _bookShelf;
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
		}

		Transform _Transform = bookShelf.Shelf.transform.parent;

		if (_Transform != null)
		{
			if (TransformToBookShelf.ContainsKey(_Transform))
			{
				bookShelf.ObscuredBy = TransformToBookShelf[_Transform];
			}
			else
			{
				bookShelf.ObscuredBy = PartialGeneratebookShelf(_Transform);
			}
		}
		else {
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
			bookShelf.ObscuredBy = TopSceneBookshelf;
			if (!TopSceneBookshelf.ObscuredBookShelves.Contains(bookShelf)) {
				TopSceneBookshelf.ObscuredBookShelves.Add(bookShelf);
			}
		}
		bookShelf.IsPartiallyGenerated = false;
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
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;
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
				Page.Sentences.SentenceID = Page.ASentenceID;
				Page.ASentenceID++;
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
									_sentence.KeyVariable = valueType.GetProperty("Key").GetValue(c, null);
									_sentence.ValueVariable = valueType.GetProperty("Value").GetValue(c, null);

									_sentence.ValueVariableType = valueType.GetGenericArguments()[1];
									_sentence.KeyVariableType = valueType.GetGenericArguments()[0];
									if (_sentence.KeyVariableType == null)
									{

										Logger.LogError("HEGEGEEGHELP!!");
									}
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
							if (_sentence.KeyVariableType == null) {
								Logger.LogError("HEGEGEEGHELP!!");
							}

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
		//public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();
		public bool IsPartiallyGenerated = true;
		public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();

		public BookShelf ObscuredBy;
		public List<Book> HeldBooks = new List<Book>();

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
		public string AssemblyQualifiedName;
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
