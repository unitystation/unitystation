using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;

public class BookshelfViewer : MonoBehaviour
{
	uint ListTop = 0;
	uint ListBottom = 2;
	public HashSet<ulong> WaitingOn = new HashSet<ulong>();
	public bool IsUnInitialised = true;
	public Dictionary<ulong, uint> IDToLocation = new Dictionary<ulong, uint>();
	public GameObject DynamicPanel;

	public SingleBookshelf UISingleBookshelf;

	public List<SingleBookshelf> BookshelfList = new List<SingleBookshelf>();

	private VariableViewerNetworking.NetFriendlyBookShelf _BookShelfIn;

	public VariableViewerNetworking.NetFriendlyBookShelf BookShelfIn
	{
		get { return _BookShelfIn; }
		set
		{
			_BookShelfIn = value;
			BookShelfInSetUp();
			return;
		}
	}

	private VariableViewerNetworking.NetFriendlyBookShelfView _BookShelfView;

	public VariableViewerNetworking.NetFriendlyBookShelfView BookShelfView
	{
		get { return _BookShelfView; }
		set
		{
			_BookShelfView = value;
			ValueSetUp();
			return;
		}
	}

	public void BookShelfInSetUp()
	{
		if (WaitingOn.Contains(_BookShelfIn.ID))
		{
			WaitingOn.Remove(_BookShelfIn.ID);
			BookshelfList[(int) IDToLocation[_BookShelfIn.ID]].BookShelfView = _BookShelfIn;
		}
	}

	public void ValueSetUp()
	{
		ListTop = 0;
		if (IsUnInitialised)
		{
			IsUnInitialised = false;
			for (uint i = 0; i < 3; i++)
			{
				SingleBookshelf SingleBookEntry = Instantiate(UISingleBookshelf) as SingleBookshelf;
				SingleBookEntry.transform.SetParent(DynamicPanel.transform);
				SingleBookEntry.transform.localScale = Vector3.one;
				BookshelfList.Add(SingleBookEntry);
				BookshelfList[(int) i].gameObject.SetActive(false);
			}
		}

		for (uint i = 0; i < 3; i++)
		{
			if (_BookShelfView.HeldShelfIDs.Length > (i))
			{
				BookshelfList[(int) i].gameObject.SetActive(true);
				WaitingOn.Add(_BookShelfView.HeldShelfIDs[i].ID);
				IDToLocation[_BookShelfView.HeldShelfIDs[i].ID] = i;
				RequestBookshelfNetMessage.Send(_BookShelfView.HeldShelfIDs[i].ID, false,
					ServerData.UserID, PlayerList.Instance.AdminToken);
				ListBottom = i;
			}
			else
			{
				BookshelfList[(int) i].gameObject.SetActive(false);
			}
		}
	}

	public void PageUp()
	{
		if (ListTop != 0)
		{
			BookshelfList[2].BookShelfView = BookshelfList[1].BookShelfView;
			BookshelfList[1].BookShelfView = BookshelfList[0].BookShelfView;
			ListTop--;
			ListBottom--;
			WaitingOn.Add(_BookShelfView.HeldShelfIDs[(int) ListTop].ID);
			IDToLocation[_BookShelfView.HeldShelfIDs[ListTop].ID] = 0;
			RequestBookshelfNetMessage.Send(_BookShelfView.HeldShelfIDs[ListTop].ID,
				false, ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public void PageDown()
	{
		if (!(_BookShelfView.HeldShelfIDs.Length <= (ListBottom + 1)))
		{
			BookshelfList[0].BookShelfView = BookshelfList[1].BookShelfView;
			BookshelfList[1].BookShelfView = BookshelfList[2].BookShelfView;
			ListTop++;
			ListBottom++;
			WaitingOn.Add(_BookShelfView.HeldShelfIDs[(int) ListBottom].ID);
			IDToLocation[_BookShelfView.HeldShelfIDs[ListBottom].ID] = 2;
			RequestBookshelfNetMessage.Send(_BookShelfView.HeldShelfIDs[ListBottom].ID, false,
				ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public void GoToObscuringBookshelf()
	{
		RequestBookshelfNetMessage.Send(_BookShelfView.ID, true,
			ServerData.UserID, PlayerList.Instance.AdminToken);
	}

	public void Refresh()
	{
		if (_BookShelfView.HeldShelfIDs.Length > 0)
		{
			RequestBookshelfNetMessage.Send(_BookShelfView.HeldShelfIDs[0].ID, true,
				ServerData.UserID, PlayerList.Instance.AdminToken);
		}
	}

	public void Close()
	{
		gameObject.SetActive(false);
	}


	void OnEnable()
	{
		EventManager.AddHandler(EVENT.RoundEnded, Reset);
	}

	void OnDisable()
	{
		EventManager.RemoveHandler(EVENT.RoundEnded, Reset);
	}

	public void Reset()
	{
		ListTop = 0;
		ListBottom = 2;
		IDToLocation.Clear();
		WaitingOn.Clear();
		Close();
	}
}