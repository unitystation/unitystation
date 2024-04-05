using System;
using System.Collections.Generic;
using Logs;
using Messages.Client.Admin;
using Player;
using UnityEngine;
using TMPro;


namespace AdminTools.VariableViewer
{
	public class UI_BooksInBookshelf : MonoBehaviour
	{
		public TMP_InputField ShelfInformation;

		public uint maxBooks = 24;
		public HeldBook UIHeldBook;
		public GameObject booksPanel;
		public uint CurrentlyVisible = 0;


		public GameObject ButtonLeft;
		public GameObject ButtonRight;

		public List<HeldBook> VisibleBooks = new List<HeldBook>();
		public List<List<HeldBook>> TotalBooks = new List<List<HeldBook>>();
		public List<HeldBook> PooledBooks = new List<HeldBook>();

		public GameGizmoSquare GameGizmoSquare;

		public GameObject CurrentlyTracking;

		private VariableViewerNetworking.NetFriendlyBookShelf _BookShelfView;

		public VariableViewerNetworking.NetFriendlyBookShelf BookShelfView => _BookShelfView;

		public bool Inited = false;

		public void Awake()
		{
			ShelfInformation.onEndEdit.AddListener(RenameObject);

		}

		public void RenameObject(string NewName)
		{
			if (_BookShelfView != null)
			{
				RequestRenameVVObject.Send(_BookShelfView.ID, NewName, UISendToClientToggle.toggle);
			}
		}


		private void OnEnable()
		{
			if (Inited == false)
			{
				Inited = true;
				EventManager.AddHandler(Event.RoundEnded, PoolBooks);
			}

			if (CurrentlyTracking != null)
			{
				if (GameGizmoSquare == null)
				{
					GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(CurrentlyTracking, Vector3.zero, Color.cyan);
				}
				else
				{
					GameGizmoSquare.TrackingObject = CurrentlyTracking;
				}
			}
			else
			{
				GameGizmoSquare.OrNull()?.Remove();
			}
		}

		private void OnDisable()
		{
			GameGizmoSquare.OrNull()?.Remove();
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

		public void ValueSetUp(VariableViewerNetworking.NetFriendlyBookShelf BookShelfView, GameObject ObjectorMark, bool Teleport )
		{
			CurrentlyTracking = ObjectorMark;
			if (ObjectorMark != null)
			{
				if (GameGizmoSquare == null)
				{
					GameGizmoSquare = GameGizmomanager.AddNewSquareStaticClient(ObjectorMark, Vector3.zero, Color.cyan);
				}
				else
				{
					GameGizmoSquare.TrackingObject = ObjectorMark;
				}
			}
			else
			{
				GameGizmoSquare.OrNull()?.Remove();
			}


			if (Teleport && ObjectorMark != null)
			{
				var GhostMove = PlayerManager.LocalPlayerObject.GetComponent<GhostMove>();
				if (GhostMove != null)
				{
					GhostMove.CMDSetServerPosition(ObjectorMark.AssumedWorldPosServer());
					var Orbit = GhostMove.GetComponent<GhostOrbit>();
					Orbit.CmdServerOrbit(ObjectorMark);
				}
				else
				{
					RequestAdminTeleport.Send(
						null,
						null,
						RequestAdminTeleport.OpperationList.TeleportAdmin,
						false,
						ObjectorMark.AssumedWorldPosServer()
					);
				}
			}



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
				if (i >= maxBooks)
				{
					SingleBookEntry.gameObject.SetActive(false);
					int bookSetNumber = (int)Math.Floor((decimal)((float)i / maxBooks));
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

			if (TotalBooks.Count > 0)
			{
				ButtonRight.SetActive(true);
			}
			else
			{
				ButtonRight.SetActive(false);
			}
			ButtonLeft.SetActive(false);
		}

		public void BooksLeft()
		{
			int tint = (int)CurrentlyVisible;
			if ((tint - 1) >= 0)
			{
				CurrentlyVisible--;
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(false);
				}
				VisibleBooks = TotalBooks[(int)CurrentlyVisible];
				foreach (var book in VisibleBooks)
				{
					book.gameObject.SetActive(true);
				}

				if (CurrentlyVisible == 0)
				{
					ButtonLeft.SetActive(false);
				}
				else
				{
					ButtonLeft.SetActive(true);
				}

				if (CurrentlyVisible < (TotalBooks.Count - 1))
				{
					ButtonRight.SetActive(true);
				}
				else
				{
					ButtonRight.SetActive(false);
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

				if (CurrentlyVisible < (TotalBooks.Count - 1))
				{
					ButtonRight.SetActive(true);
				}
				else
				{
					ButtonRight.SetActive(false);
				}

				if (CurrentlyVisible == 0)
				{
					ButtonLeft.SetActive(false);
				}
				else
				{
					ButtonLeft.SetActive(true);
				}
			}
		}
	}
}
