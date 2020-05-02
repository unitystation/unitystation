using System;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;

public class GUI_VariableViewer : MonoBehaviour
{
	private ulong _ID;
	public ulong ID
	{
		get { return _ID; }
		set
		{
			boookID.text = "Book ID > " + value.ToString();
			_ID = value;
		}
	}
	private string _Title;
	public string Title
	{
		get
		{
			return _Title;

		}
		set
		{
			boookTitle.text = "Title > " + value.ToString();
			_Title = value;
			return;
		}
	}

	private bool _IsEnabled;
	public bool IsEnabled
	{
		get { return _IsEnabled; }
		set
		{
			boookIsEnabled.text = "IsEnabled > " + value.ToString();
			_IsEnabled = value;
		}
	}

	public VariableViewerNetworking.NetFriendlyBook CurrentlyOpenBook; //{get {} set{} };
	public TMP_Text boookID;
	public TMP_Text boookTitle;
	public TMP_Text boookIsEnabled;
	public GameObject LeftArrow;
	public GameObject RightArrow;
	public GameObject HistoryForward;
	public GameObject HistoryBackwards;
	public List<ulong> History = new List<ulong>();
	public int HistoryLocation = -1;
	public bool NotModifyingHistory;
	public List<GUI_PageEntry> PooledPages = new List<GUI_PageEntry>();
	public List<List<GUI_PageEntry>> PagesInBook = new List<List<GUI_PageEntry>>();
	public List<GUI_PageEntry> CurrentlyOpen;
	public int intCurrentlyOpen = 0;
	public int PresentPagesCount = 0;
	public int MaximumPerTwoPages = 40;
	public GameObject PagePanel;
	public GUI_PageEntry PageEntryPrefab;
	public GameObject window;
	public GameObject bookshelfWindow;

	public void Start()
	{
		window.SetActive(false);
	}
	public void Open()
	{
		window.SetActive(true);
		bookshelfWindow.SetActive(true);
	}
	public void Close()
	{
		window.SetActive(false);
	}
	public void Refresh()
	{
		OpenBookIDNetMessage.Send(CurrentlyOpenBook.ID, ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void NextBook()
	{
		int tint = HistoryLocation;
		if ((tint + 1) <= History.Count)
		{
			HistoryLocation = HistoryLocation + 1;
			OpenBookIDNetMessage.Send(History[HistoryLocation], ServerData.UserID, PlayerList.Instance.AdminToken);
			NotModifyingHistory = true;
			if (HistoryLocation + 1 >= History.Count)
			{
				HistoryForward.SetActive(false);
			}
		}
	}

	public void Previousbook()
	{
		int tint = HistoryLocation;
		if ((tint - 1) >= 0)
		{
			HistoryLocation = HistoryLocation - 1;
			NotModifyingHistory = true;
			OpenBookIDNetMessage.Send(History[HistoryLocation], ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public void PageRight()
	{
		int tint = intCurrentlyOpen;
		if ((tint + 1) <= PagesInBook.Count)
		{
			intCurrentlyOpen++;
			foreach (var page in CurrentlyOpen)
			{
				page.gameObject.SetActive(false);
			}
			CurrentlyOpen = PagesInBook[intCurrentlyOpen];
			foreach (var page in CurrentlyOpen)
			{
				page.gameObject.SetActive(true);
			}
			LeftArrow.gameObject.SetActive(true);
			if (intCurrentlyOpen + 1 >= PagesInBook.Count)
			{
				RightArrow.gameObject.SetActive(false);
			}
		}
		else
		{

			RightArrow.gameObject.SetActive(false);
		}
	}

	public void PageLeft()
	{
		int tint = intCurrentlyOpen;
		if ((tint - 1) >= 0)
		{
			intCurrentlyOpen = intCurrentlyOpen - 1;
			foreach (var page in CurrentlyOpen)
			{
				page.gameObject.SetActive(false);
			}
			CurrentlyOpen = PagesInBook[intCurrentlyOpen];
			foreach (var page in CurrentlyOpen)
			{
				page.gameObject.SetActive(true);
			}
			RightArrow.gameObject.SetActive(true);
			if (intCurrentlyOpen == 0)
			{
				LeftArrow.gameObject.SetActive(false);
			}
		}
		else
		{
			LeftArrow.gameObject.SetActive(false);
		}
	}

	public void ReceiveBook(VariableViewerNetworking.NetFriendlyBook Book)
	{
		Pool();
		ID = Book.ID;
		Title = Book.Title;
		PresentPagesCount = 0;

		CurrentlyOpenBook = Book;

		if (History.Count > 0)
		{
			HistoryBackwards.SetActive(true);
		}
		if (!NotModifyingHistory)
		{
			if ((History.Count - 1) != HistoryLocation)
			{
				ListExtensions.RemoveAtIndexForwards(History, HistoryLocation);
			}
			History.Add(Book.ID);
			HistoryLocation = HistoryLocation + 1;

		}
		NotModifyingHistory = false;

		if (HistoryLocation < 1)
		{
			HistoryBackwards.SetActive(false);
		}
		else
		{
			HistoryBackwards.SetActive(true);
		}

		if (HistoryLocation + 1 < History.Count)
		{
			HistoryForward.SetActive(true);
		}
		else
		{
			HistoryForward.SetActive(false);
		}

		foreach (var page in CurrentlyOpenBook.BindedPages)
		{
			GUI_PageEntry PageEntry;
			if (PooledPages.Count > 0)
			{
				PageEntry = PooledPages[0];
				PooledPages.RemoveAt(0);
				PageEntry.gameObject.SetActive(true);
				PageEntry.transform.SetParent(PagePanel.transform, true);
			}
			else
			{
				PageEntry = Instantiate(PageEntryPrefab) as GUI_PageEntry;
				PageEntry.transform.SetParent(PagePanel.transform, true);
				PageEntry.transform.localScale = Vector3.one;
			}

			PageEntry.Page = page;
			//Logger.Log(JsonConvert.SerializeObject(page));
			if (PresentPagesCount > MaximumPerTwoPages)
			{
				PageEntry.gameObject.SetActive(false);
				int PageSetNumber = (int) Math.Floor((decimal) (PresentPagesCount / MaximumPerTwoPages));

				if ((PagesInBook.Count - 1) != PageSetNumber)
				{
					PagesInBook.Add(new List<GUI_PageEntry>());
				}
				PagesInBook[PageSetNumber].Add(PageEntry);

			}
			else
			{
				PagesInBook[0].Add(PageEntry);
			}
			PresentPagesCount++;
		}
		CurrentlyOpen = PagesInBook[0];
		intCurrentlyOpen = 0;
		if (PagesInBook.Count > 1)
		{
			RightArrow.SetActive(true);
		}
	}

	public void PoolPageEntry(GUI_PageEntry PageEntry)
	{
		PageEntry.gameObject.SetActive(false);
		PageEntry.transform.SetParent(this.transform, true);
		if (!PooledPages.Contains(PageEntry))
		{
			PooledPages.Add(PageEntry);
		}
	}


	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, Reset);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, Reset);
	}

	public void Pool()
	{
		ID = 0;
		Title = "Title";
		PresentPagesCount = 0;
		RightArrow.SetActive(false);
		LeftArrow.SetActive(false);
		VVUIElementHandler.Pool();
		foreach (var ListOfPages in PagesInBook)
		{
			foreach (var Page in ListOfPages)
			{
				PoolPageEntry(Page);
			}
		}
		foreach (var Page in CurrentlyOpen)
		{
			PoolPageEntry(Page);
		}

		PagesInBook.Clear();
		PagesInBook.Add(new List<GUI_PageEntry>());
		CurrentlyOpen.Clear();
	}

	public void Reset()
	{
		Pool();
		History.Clear();
		HistoryLocation = -1;
		Close();
	}
}