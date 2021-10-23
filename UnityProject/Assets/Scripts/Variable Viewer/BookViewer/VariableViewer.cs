using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using System.Text;
using System.Linq;
using Messages.Server.VariableViewer;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = System.Object;


// TODO
// pool books

public static class VariableViewer
{
	public static void ProcessTile(Vector3 Location, GameObject WhoBy)
	{
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

	}

	public static void ProcessTransform(Transform transform, GameObject WhoBy)
	{
		Librarian.library.TraverseHierarchy();
		Librarian.Library.LibraryBookShelf BookShelf;
		if (Librarian.TransformToBookShelf.ContainsKey(transform))
		{
			BookShelf = Librarian.TransformToBookShelf[transform];
		}
		else
		{
			BookShelf = Librarian.Library.LibraryBookShelf.PartialGenerateLibraryBookShelf(transform);
		}



		BookShelf.PopulateBookShelf();
		SendBookShelfToClient(BookShelf,WhoBy);
		LibraryNetMessage.Send(Librarian.library, WhoBy);

	}

	public static void RequestHierarchy(GameObject WhoBy)
	{
		Librarian.library.TraverseHierarchy();
		LibraryNetMessage.Send(Librarian.library, WhoBy);
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

	public static void SendBookShelfToClient(Librarian.Library.LibraryBookShelf BookShelf, GameObject ToWho)
	{
		SubBookshelfNetMessage.Send(BookShelf, ToWho);
	}

	//Receive from Client side
	public static void RequestOpenPageValue(ulong PageID, uint SentenceID, bool IsSentence, bool iskey, GameObject ToWho)
	{
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
			var Bookshelf = Librarian.IDToBookShelf[BookshelfID];
			if (IsNewbookBookshelf)
			{
				if (Bookshelf.Shelf == null)
				{
					Logger.LogError("Bookshelf has been destroyed > " + BookshelfID, Category.VariableViewer);
					return;
				}

				if (Bookshelf.IsPartiallyGenerated)
				{
					Bookshelf.PopulateBookShelf();
				}
				SubBookshelfNetMessage.Send(Bookshelf, WhoBy);
			}
			else
			{
				SubBookshelfNetMessage.Send(Bookshelf, WhoBy);
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

	public static void RequestInvokeFunction(ulong PageID, GameObject WhoBy, string AdminId)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				WhoBy.name + " Invoked " + Librarian.IDToPage[PageID].VariableName + " on " +  Librarian.IDToPage[PageID].BindedTo.Title
				, AdminId);

			Librarian.IDToPage[PageID].Invoke();
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
	public static Library library = new Library();

	public static Dictionary<ulong, Library.LibraryBookShelf> IDToBookShelf = new Dictionary<ulong, Library.LibraryBookShelf>();
	public static Dictionary<ulong, Book> IDToBook = new Dictionary<ulong, Book>();
	public static Dictionary<ulong, Page> IDToPage = new Dictionary<ulong, Page>();

	public static ulong BookShelfAID = 1;
	public static ulong BookAID = 1;
	public static ulong PageAID = 1;

	public static Dictionary<Transform, Library.LibraryBookShelf> TransformToBookShelf = new Dictionary<Transform, Library.LibraryBookShelf>();
	public static Dictionary<MonoBehaviour, Book> MonoBehaviourToBook = new Dictionary<MonoBehaviour, Book>();
	public static Dictionary<object, Book> ObjectToBook = new Dictionary<object, Book>();

	public static Type TupleTypeReference = Type.GetType("System.ITuple, mscorlib");

	public static void Reset()
	{

		library = new Library();
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


	// public static BookShelf GenerateCustomBookCase(List<BookShelf> BookShelfs)
	// {
	// 	BookShelf Customshelf = new BookShelf();
	// 	Customshelf.ID = BookShelfAID;
	// 	BookShelfAID++;
	// 	Customshelf.IsPartiallyGenerated = false;
	// 	Customshelf.ShelfName = "Your custom list of bookshelves";
	// 	Customshelf.ObscuredBookShelves = BookShelfs;
	// 	Customshelf.ICustomBookshelf = true;
	// 	IDToBookShelf[Customshelf.ID] = Customshelf;
	// 	return (Customshelf);
	// }
	//
	// public static BookShelf GenerateCustomBookCase(BookShelf BookShelf)
	// {
	// 	BookShelf Customshelf = new BookShelf();
	// 	Customshelf.ID = BookShelfAID;
	// 	BookShelfAID++;
	// 	Customshelf.IsPartiallyGenerated = false;
	// 	Customshelf.ShelfName = "Your custom list of bookshelves";
	// 	Customshelf.ObscuredBookShelves.Add(BookShelf);
	// 	Customshelf.ICustomBookshelf = true;
	// 	IDToBookShelf[Customshelf.ID] = Customshelf;
	// 	return (Customshelf);
	// }

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
				var attribute = Field.GetCustomAttributes(typeof(VVNote), true);
				if (attribute.Length > 0)
				{
					var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
					Page.VVHighlight = VVNoteAttributes.variableHighlightl;
				}

				GenerateSentenceValuesforSentence(Page.Sentences, Field.FieldType, Page, Script, FInfo: Field);
				Book.BindedPagesAdd(Page);
			}
		}

		if (TupleTypeReference != monoType) //Causes an error if this is not here and Tuples can not get Custom properties so it is I needed to get the properties
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

					var attribute = Properties.GetCustomAttributes(typeof(VVNote), true);
					if (attribute.Length > 0)
					{
						var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
						Page.VVHighlight = VVNoteAttributes.variableHighlightl;
					}

					GenerateSentenceValuesforSentence(Page.Sentences, Properties.PropertyType, Page, Script,
						PInfo: Properties);
					Book.BindedPagesAdd(Page);
				}
			}
		}

		var coolMethods = (monoType.GetMethods(
			BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
			BindingFlags.FlattenHierarchy
		).ToList());

		foreach (MethodInfo Method in coolMethods)
		{
			if (Method.GetParameters().Length > 0) continue;
			Page Page = new Page();
			Page.VariableName = Method.Name;
			Page.ID = PageAID;
			PageAID++;
			Page.MInfo = Method;

			if (Page.Variable == null)
			{
				Page.Variable = "null";
			}
			Page.BindedTo = Book;
			IDToPage[Page.ID] = Page;
			Page.Sentences = new Librarian.Sentence();
			Page.Sentences.SentenceID = Page.ASentenceID;
			Page.ASentenceID++;



			var attribute = Method.GetCustomAttributes(typeof(VVNote), true);
			if (attribute.Length > 0)
			{
				var VVNoteAttributes = attribute.Cast<VVNote>().ToArray()[0];
				Page.VVHighlight = VVNoteAttributes.variableHighlightl;
			}

			// GenerateSentenceValuesforSentence(Page.Sentences, Field.FieldType, Page, Script, FInfo: Field);
			Book.BindedPagesAdd(Page);
		}

		return (Book);
	}

	public static void GenerateSentenceValuesforSentence(Sentence sentence, Type VariableType, Page Page, object Script,
		FieldInfo FInfo = null, PropertyInfo PInfo = null)
	{
		if (FInfo == null && PInfo == null)
		{
			foreach (FieldInfo Field in VariableType.GetFields(
				BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static |
				BindingFlags.FlattenHierarchy
			))
			{
				if (Field.GetCustomAttributes(typeof(ObsoleteAttribute), true).Length == 0)
				{
					if (Field.FieldType.IsGenericType)
					{
						IEnumerable list = Field.GetValue(Script) as IEnumerable;
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
				if (FInfo == null)
				{
					list = PInfo.GetValue(Script) as IEnumerable; //icollection<keyvaluepair>
				}
				else
				{
					list = FInfo.GetValue(Script) as IEnumerable; //
				}

				sentence.Sentences = new List<Sentence>();
				uint count = 0;
				if (list != null)
				{
					foreach (object c in list)
					{
						if (c == null) continue;

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

	public class Library
	{
		public List<LibraryBookShelf> Roots = new List<LibraryBookShelf>();

		public Dictionary<Transform, LibraryBookShelf> TransformToBookShelves = new Dictionary<Transform,LibraryBookShelf>();
		public void TraverseHierarchy()
		{
			List<Transform> Transforms = new List<Transform>();
			foreach (var KV in Librarian.library.TransformToBookShelves)
			{
				if (KV.Key == null)
				{
					Transforms.Add(KV.Key);
				}
			}

			foreach (var TF in Transforms)
			{
				Librarian.library.TransformToBookShelves.Remove(TF);
				TransformToBookShelf.Remove(TF);
			}

			int countLoaded = SceneManager.sceneCount;
			Scene[] loadedScenes = new Scene[countLoaded];

			for (int i = 0; i < countLoaded; i++)
			{
				loadedScenes[i] = SceneManager.GetSceneAt(i);
			}

			foreach (var Scene in loadedScenes)
			{
				var roots = Scene.GetRootGameObjects();
				foreach (var root in roots)
				{
					RecursivePopulate(root.transform, null);
				}
			}

		}

		List<LibraryBookShelf> THISDestroy = new List<LibraryBookShelf>();
		List<Transform> Children = new List<Transform>();
		List<Transform> TOProcessAdd = new List<Transform>();
		List<LibraryBookShelf> TOProcessRemove = new List<LibraryBookShelf>();
		public void RecursivePopulate(Transform Object, Transform Parent)
		{
			THISDestroy.Clear();
			Children.Clear();
			TOProcessAdd.Clear();
			TOProcessRemove.Clear();

			if (TransformToBookShelves.ContainsKey(Object))
			{

				if (Object.childCount > 0)
				{
					for (int i = 0; i < Object.childCount; i++)
					{
						Children.Add(Object.GetChild(i));
					}
				}

				var libraryBookShelf =  TransformToBookShelves[Object];


				foreach (var Child in Children)
				{
					if (libraryBookShelf.Contains.Contains(Child) == false)
					{
						TOProcessAdd.Add(Child);
					}
				}

				bool DestroySelf = false;
				if (libraryBookShelf.Parent != Parent)
				{
					if (libraryBookShelf.ParentChange())
					{
						DestroySelf = true;
					}
				}


				bool hasNull = false;
				foreach (var Child in libraryBookShelf.InternalContain)
				{
					if (Child.Shelf == null)
					{
						TOProcessRemove.Add(Child);
						continue;
					}
					if (Children.Contains(Child.Shelf.transform) == false)
					{
						TOProcessRemove.Add(Child);
					}

				}


				foreach (var Child in TOProcessRemove)
				{
					libraryBookShelf.Contains.RemoveAll(item => item == null);
					if (Child.Shelf != null)
					{
						libraryBookShelf.Contains.Remove(Child.Shelf.transform);
						if (TransformToBookShelves.ContainsKey(Child.Shelf.transform))
						{
							TransformToBookShelves[Child.Shelf.transform].ParentChange();
						}
					}
					else
					{
						Child.DestroySelf();
					}

					libraryBookShelf.InternalContain.Remove(Child);

				}


				foreach (var Child in TOProcessAdd)
				{
					libraryBookShelf.Contains.Add(Child);
					Children.Add(Child);
				}


				foreach (var _Shelf in THISDestroy)
				{
					_Shelf.DestroySelf();
				}

				if (DestroySelf)
				{
					libraryBookShelf.DestroySelf();
				}

				foreach (var Child in Children.ToArray())
				{
					RecursivePopulate(Child,Object);
				}
			}
			else
			{
				if (Object.childCount > 0)
				{
					for (int i = 0; i < Object.childCount; i++)
					{
						Children.Add(Object.GetChild(i));
					}
				}

				var libraryBookShelf =  LibraryBookShelf.PartialGenerateLibraryBookShelf(Object, Parent, Children);
				TransformToBookShelves[Object] = libraryBookShelf;
				if (Parent == null)
				{
					Roots.Add(libraryBookShelf);
				}

				foreach (var Child in Children.ToArray())
				{
					RecursivePopulate(Child,Object);
				}
			}


		}


		public class LibraryBookShelf
		{
			public Transform Parent;
			public List<Transform> Contains = new List<Transform>();

			public List<LibraryBookShelf> InternalContain = new List<LibraryBookShelf>();

			public bool IsPartiallyGenerated = true;
			public List<Book> HeldBooks = new List<Book>();

			public ulong ID;
			public string ShelfName;
			public bool IsEnabled;
			public GameObject Shelf;

			public void DestroySelf()
			{
				if (Shelf != null)
				{
					Librarian.library.TransformToBookShelves.Remove(Shelf.transform);
					TransformToBookShelf.Remove(Shelf.transform);
				}
				IDToBookShelf.Remove(ID);
				Librarian.library.Roots.Remove(this);
				Contains.Clear();

			}

			public static LibraryBookShelf PartialGenerateLibraryBookShelf(Transform _Transform)
			{
				List<Transform> Children = new List<Transform>();
				if (_Transform.childCount > 0)
				{
					for (int i = 0; i < _Transform.childCount; i++)
					{
						Children.Add(_Transform.GetChild(i));
					}
				}

				return PartialGenerateLibraryBookShelf(_Transform, _Transform.parent, Children);
			}


			public static LibraryBookShelf PartialGenerateLibraryBookShelf(Transform _Transform, Transform _Parent, List<Transform> Children)
			{
				if (library.TransformToBookShelves.ContainsKey(_Transform))
				{
					return (library.TransformToBookShelves[_Transform]);
				}

				var libraryBookShelf = new LibraryBookShelf();
				libraryBookShelf.ShelfName = _Transform.gameObject.name;
				libraryBookShelf.ID = BookShelfAID;
				BookShelfAID++;
				libraryBookShelf.Shelf = _Transform.gameObject;
				libraryBookShelf.Parent = _Parent;
				if (_Parent != null)
				{
					library.TransformToBookShelves[_Parent].InternalContain.Add(libraryBookShelf);
				}

				libraryBookShelf.Contains.AddRange(Children);
				IDToBookShelf[libraryBookShelf.ID] = libraryBookShelf;
				TransformToBookShelf[libraryBookShelf.Shelf.transform] = libraryBookShelf;

				return libraryBookShelf;
			}

			public bool ParentChange()
			{
				if (Shelf == null)
				{
					return false;
				}
				Parent = Shelf.transform.parent;
				if (Shelf.transform.parent != null)
				{
					library.TransformToBookShelves[Shelf.transform.parent].InternalContain.Add(this);
				}

				return true;
			}


			public void PopulateBookShelf()
			{
				if (!this.IsPartiallyGenerated)
				{
					return;
				}
				MonoBehaviour[] scriptComponents = Shelf.GetComponents<MonoBehaviour>();
				HeldBooks.Clear();
				foreach (MonoBehaviour mono in scriptComponents)
				{
					if (mono != null)
					{
						HeldBooks.Add(Book.PartialGeneratebook(mono));
					}
				}

				this.IsPartiallyGenerated = false;

			}

			public void UpdateBookShelf()
			{
				if (IsPartiallyGenerated)
				{
					PopulateBookShelf();
					return;
				}
			}


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
					Logger.LogWarning("USE GetBindedPages()!,since these books are ungenerated ", Category.VariableViewer);
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
					Logger.LogError("Book has been destroyed!" + ID, Category.VariableViewer);
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
		public bool PCanWrite => PInfo.CanWrite;

		public FieldInfo Info;

		public uint ASentenceID;
		public Sentence Sentences;

		public MethodInfo MInfo;

		public Dictionary<uint, Sentence> IDtoSentence = new Dictionary<uint, Sentence>();
		public VVHighlight VVHighlight = VVHighlight.None;
		public override string ToString()
		{
			return (VariableName + " = " + Variable + " of   " + VariableType);
		}

		public void SetValue(string Value)
		{
			//Logger.Log(this.ToString());
			//Logger.Log(ID.ToString());
			//Logger.Log(Variable.GetType().ToString());
			try
			{
				if (PInfo != null)
				{
					PInfo.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
				}
				else if (Info != null)
				{
					Info.SetValue(BindedTo.BookClass, DeSerialiseValue(Variable, Value, Variable.GetType()));
				}

				UpdatePage();
			}
			catch (ArgumentException exception)
			{
				Logger.LogError("Catch Argument Exception for Variable Viewer " + exception.Message, Category.VariableViewer);
			}
		}

		public void Invoke()
		{
			if (MInfo != null)
			{
				MInfo.Invoke(BindedTo.BookClass, null);
			}
		}


		public static object DeSerialiseValue(object InObject, string StringVariable, Type InType)
		{
			if (VVUIElementHandler.Type2Element.ContainsKey(InType))
			{
				return (VVUIElementHandler.Type2Element[InType].DeSerialise(StringVariable));
			}
			else
			{
				if (InType.IsEnum)
				{
					//if ()
					return Enum.Parse(InObject.GetType(), StringVariable);
				}
				else
				{
					if (InType == null || InObject == null || InObject as IConvertible == null)
					{
						Logger.Log($"Can't convert {StringVariable} to {InObject.GetType()}  " +
							$"[(InType == null) = {InType == null} || (InObject == null) == {InObject == null} || (InObject as IConvertible == null) = {InObject as IConvertible == null}]", Category.VariableViewer);
						return null;
					}

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
			if (Info != null)
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
					GenerateSentenceValuesforSentence(Sentences, Info.FieldType, this, BindedTo.BookClass, FInfo: Info);
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
			if (assemblyName != "Unity" && assemblyName != "Light2D")
			{
				var assembly = Assembly.Load(assemblyName);

				// Ask that assembly to return the proper Type
				type = assembly.GetType(TypeName);
				if (type != null)
					return type;
			}

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