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
	public static HashSet<Type> DeprecatedTypes = new HashSet<Type>()
	{
		typeof(Rigidbody)
	};

	public static void PrintSomeVariables(GameObject _object)
	{
		MonoBehaviour[] scriptComponents = _object.GetComponents<MonoBehaviour>();
		Logger.Log(Librarian.GenerateBookShelf(_object).ToString());
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
}

public static class Librarian{

	public static Dictionary<ulong, BookShelf> IDToBookShelf = new Dictionary<ulong, BookShelf>();
	public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
	public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();

	public static Dictionary<BookShelf, ulong> BookShelfToID = new Dictionary<BookShelf, ulong>();
	public static Dictionary<Book, ulong> BookToID = new Dictionary<Book, ulong>();
	public static Dictionary<Page, ulong> PageToID = new Dictionary<Page, ulong>();

	public static Dictionary<Transform, ulong> TransformToBookSheleID = new Dictionary<Transform, ulong>();

	public static ulong AvailableBookShelfID= 0;
	public static ulong AvailableBookID = 0;
	public static ulong AvailablePageID = 0;
	public static BookShelf GenerateBookShelf(GameObject gameObject) {
		BookShelf bookShelf = new BookShelf();
		bookShelf.ShelfName = gameObject.name;
		bookShelf.HeldBooks = new List<Book>();
		bookShelf.Shelf = gameObject;
		bookShelf.ObscuredBookShelves = new List<BookShelf>();
		MonoBehaviour[] scriptComponents = gameObject.GetComponents<MonoBehaviour>();
		foreach (MonoBehaviour mono in scriptComponents)
		{ 
			Book book = new Book();
			book.BookClass = mono;
			book.Title = mono.ToString();
			bookShelf.HeldBooks.Add(book);
		}
		Transform[] ts = gameObject.GetComponentsInChildren<Transform>();
		foreach (Transform child in ts)
		{
			if (child != ts[0])
			{
				BookShelf _bookShelf = new BookShelf();
				_bookShelf.ShelfName = child.gameObject.name;
				_bookShelf.Shelf = child.gameObject;
				bookShelf.ObscuredBookShelves.Add(_bookShelf);
			}
		}
		return (bookShelf);
	}

	public static Book GenerateBook(MonoBehaviour mono) {
		Book book = new Book();
		book.BindedPages = new List<Page>();
		book.Title = mono.ToString();
		book.BookClass = mono;
		Type monoType = mono.GetType();
		foreach (FieldInfo method in monoType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)) 
		{
			if (method.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
			{
				Page Page = new Page();
				Page.VariableName = method.Name;
				Page.Variable = method.GetValue(mono);
				Page.VariableType = method.FieldType;
				Page.BindedTo = book;
				book.BindedPages.Add(Page);
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
				Page.BindedTo = book;
				book.BindedPages.Add(Page);
			}
		}
		return (book);
	}

	public class BookShelf
	{
		public string ShelfName;
		public bool IsEnabled;
		public GameObject Shelf;
		public List<Book> HeldBooks = new List<Book>();
		public List<BookShelf> ObscuredBookShelves = new List<BookShelf>();

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
		public string Title;
		public MonoBehaviour BookClass;
		public bool IsEnabled;
		public List<Page> BindedPages = new List<Page>();
		public bool UnGenerated = true;

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
		public override string ToString()
		{
			return (VariableName + " = " +  Variable + " of   " + VariableType);
		}
		public string VariableName;
		public object Variable;
		public Type VariableType;
		public Book BindedTo;
	}
}
