using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;
using System.Reflection;

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

	public BookNetMessage.NetFriendlyBook CurrentlyOpenBook; //{get {} set{} };

	public Text boookID;
	public Text boookTitle;
	public Text boookIsEnabled;


	public GameObject LeftArrow;
	public GameObject RightArrow;


	public void PageRight() {
		int tint = intCurrentlyOpen;
		if ((tint+1) <= PagesInBook.Count)
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
		} else {

			RightArrow.gameObject.SetActive(false);
		}
	}

	public void PageLeft()
	{
		int tint = intCurrentlyOpen;
		if ((tint-1) >= 0)
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
		else {
			LeftArrow.gameObject.SetActive(false);
		}
	}

	public List<GUI_PageEntry> PooledPages = new List<GUI_PageEntry>();
	public List<List<GUI_PageEntry>> PagesInBook = new List<List<GUI_PageEntry>>();
	public List<GUI_PageEntry> CurrentlyOpen;
	public int intCurrentlyOpen = 0;

	public void SetUpPages()
	{

	}

	public int PresentPagesCount = 0;
	public int MaximumPerTwoPages = 40;
	public GameObject PagePanel;
	public GUI_PageEntry PageEntryPrefab;
	//GUI_PageEntrypublic RadialButton ButtonPrefab;
	public void ReceiveBook( BookNetMessage.NetFriendlyBook Book )
	{
		PresentPagesCount = 0;
		RightArrow.SetActive(false);
		LeftArrow.SetActive(false);
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
		CurrentlyOpenBook = Book;


		foreach (var page in CurrentlyOpenBook.BindedPages) {
			GUI_PageEntry PageEntry;
			if (PooledPages.Count > 0)
			{
				PageEntry = PooledPages[0];
				PooledPages.RemoveAt(0);
				PageEntry.gameObject.SetActive(true);
				PageEntry.transform.SetParent(PagePanel.transform, true);
			}
			else { 
				PageEntry = Instantiate(PageEntryPrefab) as GUI_PageEntry;
				PageEntry.transform.SetParent(PagePanel.transform, true);
				PageEntry.transform.localScale = Vector3.one;
			}
		
			PageEntry.Page = page;
			//Logger.Log(PresentPagesCount.ToString());
			if (PresentPagesCount > MaximumPerTwoPages)
			{
				//Logger.Log("YAY!!");
				PageEntry.gameObject.SetActive(false);
				int PageSetNumber = (int)Math.Floor((decimal)(PresentPagesCount / MaximumPerTwoPages));
				//Logger.Log(PageSetNumber.ToString());
				if ((PagesInBook.Count- 1) != PageSetNumber)
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
		if (PagesInBook.Count > 1) {
			RightArrow.SetActive(true);
		}
	}

	public void PoolPageEntry(GUI_PageEntry PageEntry)
	{
		PageEntry.gameObject.SetActive(false);
		PageEntry.Pool();
		if (!PooledPages.Contains(PageEntry))
		{
			PooledPages.Add(PageEntry);
		}
	}


	public void Start()
	{
		UIManager.Instance.VariableViewer = this;

	}

	// Update is called once per frame
	public void Update()
	{
		//boookID.text = BookID.ToString();
	}
}


