using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Text;
using Newtonsoft.Json;


public class SingleBookshelf : MonoBehaviour
{
	public HeldBook UIHeldBook;
	public SUBBookShelf UISUBBookShelf;
	public Text ShelfInformation;
	public GameObject booksPanel;
	public GameObject SubShelfsPanel;

	public uint maxBooks = 11;
	public uint maxBookshelves = 4;

	public uint TopBookshelves = 0;
	public uint BottomBookshelves = 0;

	public uint CurrentlyVisible = 0;

	public List<SUBBookShelf> PresentSUBBookShelfs = new List<SUBBookShelf>();

	public List<HeldBook> VisibleBooks = new List<HeldBook>();
	public List<List<HeldBook>> TotalBooks = new List<List<HeldBook>>();
	public List<HeldBook> PooledBooks = new List<HeldBook>();

	private VariableViewerNetworking.NetFriendlyBookShelf _BookShelfView;
	public VariableViewerNetworking.NetFriendlyBookShelf BookShelfView
	{
		get
		{
			return _BookShelfView;

		}
		set
		{
			_BookShelfView = value;
			ValueSetUp();
			return;
		}
	}

	public void ValueSetUp()
	{
		PoolBooks();
		ShelfInformation.text = "ID > " + _BookShelfView.ID + "  " + _BookShelfView.SN;

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
			else {
				SingleBookEntry = Instantiate(UIHeldBook) as HeldBook;
				SingleBookEntry.transform.SetParent(booksPanel.transform);
				SingleBookEntry.transform.localScale = Vector3.one;
			}
			SingleBookEntry.IDANName = _BookShelfView.HB[i];
			SingleBookEntry.IMG.color = UnityEngine.Random.ColorHSV(0,1,0,1,0.8f,1);
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

		for (uint i = 0; i < PresentSUBBookShelfs.Count; i++)
		{
			if (!(i >= BookShelfView.OBS.Length))
			{
				TopBookshelves = 0;

				PresentSUBBookShelfs[(int)i].gameObject.SetActive(true);
				PresentSUBBookShelfs[(int)i].IDANName = _BookShelfView.OBS[i];
			}
			else {
				BottomBookshelves = i;
				PresentSUBBookShelfs[(int)i].gameObject.SetActive(false);
			}
		}
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
	public void Start()
	{
		for (uint i = 0; i < maxBookshelves; i++)
		{
			SUBBookShelf SingleBookEntry = Instantiate(UISUBBookShelf) as SUBBookShelf;
			SingleBookEntry.transform.SetParent(SubShelfsPanel.transform);
			SingleBookEntry.transform.localScale = Vector3.one;
			PresentSUBBookShelfs.Add(SingleBookEntry);
			SingleBookEntry.gameObject.SetActive(false);
		}
		BottomBookshelves = 3;
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

	public void BookShelveUp()
	{
		if (TopBookshelves != 0)
		{
			PresentSUBBookShelfs[3].IDANName = PresentSUBBookShelfs[2].IDANName;
			PresentSUBBookShelfs[2].IDANName = PresentSUBBookShelfs[1].IDANName;
			PresentSUBBookShelfs[1].IDANName = PresentSUBBookShelfs[0].IDANName;
			TopBookshelves--;
			BottomBookshelves--;
			PresentSUBBookShelfs[0].IDANName = _BookShelfView.OBS[TopBookshelves];
		}

	}

	public void BookShelveDown()
	{
		if (!(_BookShelfView.OBS.Length <= (BottomBookshelves + 1)))
		{
			PresentSUBBookShelfs[0].IDANName = PresentSUBBookShelfs[1].IDANName;
			PresentSUBBookShelfs[1].IDANName = PresentSUBBookShelfs[2].IDANName;
			PresentSUBBookShelfs[2].IDANName = PresentSUBBookShelfs[3].IDANName;
			TopBookshelves++;
			BottomBookshelves++;
			PresentSUBBookShelfs[3].IDANName = _BookShelfView.OBS[BottomBookshelves];
		}
	}

}
