using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;

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

	public static Librarian.BookShelf ProcessTransform(Transform transform) {
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
			if (Book.UnGenerated) {
				Book = Librarian.PopulateBook(Book);
			}
			SendBookToClient(Book);
		}
		else {
			Logger.LogError("book ID has not been generated  BookID > " + BookID);
		}

	}

	public static void SendBookToClient(Librarian.Book Book) { 
		//Send book
	}

	public static void SendBookShelfToClient(Librarian.BookShelf BookShelf)
	{
		//Send BookShelf
	}



	public static void PrintSomeVariables(GameObject _object)
	{
		
		var bob = ProcessTransform(_object.transform);

		var booke = bob.GetHeldBooks();
		Logger.Log(booke.Count.ToString());
		var bookeE = booke[(booke.Count - 1) ];
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
	//Client side


}

public static class Librarian{

	public static Dictionary<ulong, BookShelf> IDToBookShelf = new Dictionary<ulong, BookShelf>();
	public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
	public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();


	public static ulong BookShelfAID = 0;
	public static ulong BookAID = 0;
	public static ulong PageAID = 0;

	public static Dictionary<Transform, BookShelf> TransformToBookShelf = new Dictionary<Transform, BookShelf>();

	//public static Dictionary<Client, BookShelf> Customshelfs = new Dictionary<Client, BookShelf>
	public static BookShelf Customshelf;

	//public static ulong AvailableBookShelfID= 0;
	//public static ulong AvailableBookID = 0;
	//public static ulong AvailablePageID = 0;

	public static BookShelf GenerateCustomBookCase(List<BookShelf> BookShelfs) {
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

	//public static BookShelf GenerateBookShelf(GameObject gameObject) {
	//	BookShelf bookShelf = new BookShelf();
	//	bookShelf.ID = BookShelfAID;
	//	BookShelfAID++;
	//	bookShelf.ShelfName = gameObject.name;
	//	bookShelf.Shelf = gameObject;
	//	bookShelf.IsEnabled = gameObject.activeInHierarchy;
	//	IDToBookShelf[bookShelf.ID] = bookShelf;
	//	TransformToBookShelf[bookShelf.Shelf.transform] = bookShelf;


	//	MonoBehaviour[] scriptComponents = gameObject.GetComponents<MonoBehaviour>();
	//	foreach (MonoBehaviour mono in scriptComponents)
	//	{
	//		bookShelf.HeldBooksAdd(PartialGeneratebook(mono));
	//	}
	//	Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
	//	foreach (Transform child in ts)
	//	{
	//		if (child != ts[0])
	//		{
	//			BookShelf _bookShelf = new BookShelf();
	//			if (TransformToBookShelf.ContainsKey(child))
	//			{
	//				_bookShelf = TransformToBookShelf[child];
	//			}
	//			else { 
	//				_bookShelf = PartialGeneratebookShelf(child);
	//			}
	
	//			bookShelf.ObscuredBookShelves.Add(_bookShelf);
	//		}
	//	}
	//	Transform _Transform = bookShelf.Shelf.transform.parent;
	//	if (TransformToBookShelf.ContainsKey(_Transform))
	//	{
	//		bookShelf.ObscuredBy = TransformToBookShelf[_Transform];
	//	}
	//	else
	//	{
	//		bookShelf.ObscuredBy = PartialGeneratebookShelf(_Transform);
	//	}

	//	return (bookShelf);
	//}

	public static Book PartialGeneratebook(MonoBehaviour mono)
	{ 
		Book book = new Book();
		book.ID = BookAID;
		BookAID++;
		book.BookClass = mono;
		book.Title = mono.ToString();
		book.IsEnabled = mono.isActiveAndEnabled;
		IDToBook[book.ID] = book;

		return (book);
	}

	public static BookShelf PartialGeneratebookShelf(Transform _Transform) {
		BookShelf _bookShelf = new BookShelf();
		_bookShelf.ShelfName = _Transform.gameObject.name;
		_bookShelf.ID = BookShelfAID;
		BookShelfAID++;
		_bookShelf.Shelf = _Transform.gameObject;

		IDToBookShelf[_bookShelf.ID] = _bookShelf;
		TransformToBookShelf[_bookShelf.Shelf.transform] = _bookShelf;
		return (_bookShelf);
	}

	public static BookShelf PopulateBookShelf(BookShelf bookShelf) {
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


	public static Book PopulateBook(Book book) { 
		var mono = book.BookClass;

		Type monoType = mono.GetType();
		foreach (FieldInfo method in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.ID = PageAID;
				PageAID++;
				Page.Variable = method.GetValue(mono);
				if (Page.Variable == null) {
					Page.Variable = "null";
				}
				Page.VariableType = method.FieldType;
				//Page.BindedTo = book; unneeded?

				IDToPage[Page.ID] = Page;

				book.BindedPagesAdd(Page);
			}
		}

		foreach (PropertyInfo method in monoType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
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
				//Page.BindedTo = book; unneeded?
				IDToPage[Page.ID] = Page;

				book.BindedPagesAdd(Page);
			}
		}
		return (book);
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
				if (HeldBooksUnGenerated) {
					Logger.LogWarning("USE GetHeldBooks()!,since these books are ungenerated ");
				}
				return _HeldBooks;}
			set { _HeldBooks = value; }
		}
		bool HeldBooksUnGenerated = true;

		public List<Book> GetHeldBooks() { 
			if (HeldBooksUnGenerated)
			{
				PopulateBookShelf(this);
				HeldBooksUnGenerated = false;
			}
			return (_HeldBooks);
		}

		public void HeldBooksAdd(Book book) {
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

		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}
		//public Book BindedTo; unneeded?
	}
}
