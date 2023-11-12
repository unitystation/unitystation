using System.Collections;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using System.Text;
using System.Linq;
using Logs;
using Messages.Server.VariableViewer;
using SecureStuff;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;
using Object = System.Object;


// TODO
// pool books

public static class VariableViewer
{
	public static void ProcessTile(Vector3 Location, GameObject WhoBy)
	{
		Vector3Int worldPosInt = Location.RoundTo2Int().To3Int();
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

	public static void ProcessTransform(Transform transform, GameObject WhoBy, bool RefreshHierarchy = false)
	{
		if (Librarian.library.TransformToBookShelves.Count == 0)
		{
			Librarian.library.TraverseHierarchy();
			RefreshHierarchy = true;
		}

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
		if (RefreshHierarchy)
		{
			LibraryNetMessage.Send(Librarian.library, WhoBy);
		}

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
			Loggy.LogError("book ID has not been generated  BookID > " + BookID, Category.VariableViewer);
		}
	}

	public static void SendBookToClient(Librarian.Book Book, GameObject ToWho)
	{
		if (Book.UnGenerated == false)
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
						Loggy.LogWarning("Trying to process page value as book PageID > " + PageID,
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
			Loggy.LogError("Page ID has not been generated PageID > " + PageID, Category.VariableViewer);
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
					Loggy.LogError("Bookshelf has been destroyed > " + BookshelfID, Category.VariableViewer);
					Librarian.library.TraverseHierarchy();
					LibraryNetMessage.Send(Librarian.library, WhoBy);
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
			Loggy.LogError("Bookshelf ID has not been generated BookshelfID > " + BookshelfID,
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
			Loggy.LogError("Book ID has not been generated Book ID > " + BookID, Category.VariableViewer);
		}
	}


	public static void RequestChangeVariable(ulong PageID, string ChangeTo, bool SendToClient, GameObject WhoBy, string AdminId)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
				WhoBy.name + " Modified " + Librarian.IDToPage[PageID].VariableName + " on " +  Librarian.IDToPage[PageID].BindedTo.Title
				+ " From " + VVUIElementHandler.Serialise(Librarian.IDToPage[PageID].Variable, Librarian.IDToPage[PageID].VariableType) + " to "+ ChangeTo
				+ " with Send to clients? " + SendToClient, AdminId);
			Librarian.IDToPage[PageID].SetValue(ChangeTo);
			if (SendToClient)
			{
				var monoBehaviour = (Librarian.IDToPage[PageID].BindedTo.BookClass as Component);
				UpdateClientValue.Send(ChangeTo, Librarian.IDToPage[PageID].VariableName,
					TypeDescriptor.GetClassName(monoBehaviour),
					monoBehaviour.gameObject, false);
			}
		}
		else
		{
			Loggy.LogError("Page ID has not been generated Page ID > " + PageID, Category.VariableViewer);
		}
	}

	public static void RequestInvokeFunction(ulong PageID, bool SendToClient, GameObject WhoBy, string AdminId)
	{
		if (Librarian.IDToPage.ContainsKey(PageID))
		{
			UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
				WhoBy.name + " Invoked " + Librarian.IDToPage[PageID].VariableName + " on " +  Librarian.IDToPage[PageID].BindedTo.Title
				, AdminId);

			Librarian.IDToPage[PageID].Invoke();
			if (SendToClient)
			{
				var monoBehaviour = (Librarian.IDToPage[PageID].BindedTo.BookClass as Component);
				UpdateClientValue.Send("", Librarian.IDToPage[PageID].VariableName,
					TypeDescriptor.GetClassName(monoBehaviour),
					monoBehaviour.gameObject, true);
			}
		}
		else
		{
			Loggy.LogError("Page ID has not been generated Page ID > " + PageID, Category.VariableViewer);
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



public enum DisplayValueType
{
	Bools,
	Ints,
	Floats,
	Strings,
	Classes,
}
