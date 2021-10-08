using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


namespace AdminTools.VariableViewer
{
	public class UI_BooksInBookshelf : MonoBehaviour
	{
		public TMP_Text ShelfInformation;
		public uint maxBooks = 11;
		public HeldBook UIHeldBook;
		public GameObject booksPanel;
		public uint CurrentlyVisible = 0;

		public List<HeldBook> VisibleBooks = new List<HeldBook>();
		public List<List<HeldBook>> TotalBooks = new List<List<HeldBook>>();
		public List<HeldBook> PooledBooks = new List<HeldBook>();

		private VariableViewerNetworking.NetFriendlyBookShelf _BookShelfView;

		public VariableViewerNetworking.NetFriendlyBookShelf BookShelfView => _BookShelfView;

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, PoolBooks);
		}

		public void PoolBooks()
		{
			foreach (var books in TotalBooks)
			{
				foreach (var book in books)
				{
					book.gameObject.SetActive(false);
					PooledBooks.Add(book);
				}
			}
			TotalBooks.Clear();
			VisibleBooks.Clear();
			TotalBooks.Add(new List<HeldBook>());
		}

		public void ValueSetUp(VariableViewerNetworking.NetFriendlyBookShelf BookShelfView)
		{
			_BookShelfView = BookShelfView;
			UIManager.Instance.LibraryUI.Refresh();
			PoolBooks();
			ShelfInformation.text = _BookShelfView.SN;

			for (int i = 0; i < _BookShelfView.HB.Length; i++)
			{
				HeldBook SingleBookEntry;
				if (PooledBooks.Count > 0)
				{
					SingleBookEntry = PooledBooks[0];
					PooledBooks.RemoveAt(0);
					SingleBookEntry.gameObject.SetActive(true);
					SingleBookEntry.transform.SetParent(booksPanel.transform, true);
				}
				else
				{
					SingleBookEntry = Instantiate(UIHeldBook);
					SingleBookEntry.transform.SetParent(booksPanel.transform);
					SingleBookEntry.transform.localScale = Vector3.one;
				}

				SingleBookEntry.IDANName = _BookShelfView.HB[i];
				SingleBookEntry.IMG.color = UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0.8f, 1);
				if (i > maxBooks)
				{
					SingleBookEntry.gameObject.SetActive(false);
					int bookSetNumber = (int)Math.Floor((decimal)(i / maxBooks));
					if ((TotalBooks.Count - 1) != bookSetNumber)
					{
						TotalBooks.Add(new List<HeldBook>());
					}

					TotalBooks[bookSetNumber].Add(SingleBookEntry);
				}
				else
				{
					TotalBooks[0].Add(SingleBookEntry);
				}
			}

			VisibleBooks = TotalBooks[0];
			CurrentlyVisible = 0;
		}

		public void BooksLeft()
		{
			int tint = (int)CurrentlyVisible;
			if ((tint - 1) >= 0)
			{
				CurrentlyVisible = CurrentlyVisible - 1;
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(false);
				}
				VisibleBooks = TotalBooks[(int)CurrentlyVisible];
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(true);
				}
			}
		}

		public void BooksRight()
		{
			int tint = (int)CurrentlyVisible;
			if ((tint + 1) < (TotalBooks.Count))
			{
				CurrentlyVisible++;
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(false);
				}
				VisibleBooks = TotalBooks[(int)CurrentlyVisible];
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(true);
				}
			}
		}
	}
}
