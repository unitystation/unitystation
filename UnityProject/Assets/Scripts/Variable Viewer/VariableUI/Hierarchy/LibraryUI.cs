using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class LibraryUI : MonoBehaviour
	{
		public Dictionary<ulong, VariableViewerNetworking.NetFriendlyHierarchyBookShelf> IDtoBookShelves
				= new Dictionary<ulong, VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();

		public Dictionary<ulong, HierarchyEntry> IDtoHierarchyEntry = new Dictionary<ulong, HierarchyEntry>();

		public List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> Roots
				= new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();

		public List<HierarchyEntry> HierarchyEntryRoots = new List<HierarchyEntry>();

		public List<HierarchyEntry> OpenHierarchys = new List<HierarchyEntry>();

		public Queue<HierarchyEntry> PoolHierarchys = new Queue<HierarchyEntry>();

		public HierarchyEntry SpawnPrefab;

		public Transform PoolHolder;

		public Transform RootSpace;

		public List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> THisCompressedHierarchy
				= new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();

		public TMP_InputField InputField;

		private void OnEnable()
		{
			EventManager.AddHandler(Event.RoundEnded, Reset);
		}

		public void Reset()
		{
			SetUp(new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>());
		}

		public void NetRefresh()
		{
			RequestRefreshHierarchy.Send();
		}

		public HierarchyEntry GethierarchyEntry()
		{
			if (PoolHierarchys.Count == 0)
			{
				var hierarchyEntry = Instantiate(SpawnPrefab, PoolHolder);
				OpenHierarchys.Add(hierarchyEntry);
				return hierarchyEntry;
			}
			else
			{
				var hierarchyEntry = PoolHierarchys.Dequeue();
				OpenHierarchys.Add(hierarchyEntry);
				return hierarchyEntry;
			}
		}

		public void Search()
		{
			ClearHierarchy();
			var SearchString = InputField.text.ToLower();
			List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> ToShow = new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();
			foreach (var netFriendly in THisCompressedHierarchy)
			{
				if (netFriendly.Nm.ToLower().Contains(SearchString))
				{
					ToShow.Add(netFriendly);
					if (ToShow.Count > 300)
					{
						break;
					}
				}
			}


			foreach (var root in ToShow)
			{
				HierarchyEntry HierarchyEntry = GethierarchyEntry();
				HierarchyEntry.transform.SetParent(RootSpace);
				HierarchyEntry.gameObject.SetActive(true);
				HierarchyEntry.SetThis(root);
				HierarchyEntry.transform.localScale = Vector3.one;
				IDtoHierarchyEntry[root.ID] = HierarchyEntry;
				//RecursivPopulate(root);
			}


		}

		public void SetUp(List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> CompressedHierarchy)
		{
			Roots.Clear();
			IDtoBookShelves.Clear();
			THisCompressedHierarchy.Clear();
			THisCompressedHierarchy.AddRange(CompressedHierarchy);
			//Logger.Log("CompressedHierarchy Count > " + CompressedHierarchy.Count);
			foreach (var Compressed in CompressedHierarchy)
			{
				IDtoBookShelves[Compressed.ID] = Compressed;
			}

			foreach (var Compressed in CompressedHierarchy)
			{
				if (Compressed.PID == 0)
				{
					Roots.Add(Compressed);
				}
				else
				{
					Compressed.SetParent(IDtoBookShelves[Compressed.PID]);
					IDtoBookShelves[Compressed.PID].GetChildrenList().Add(Compressed);
				}
			}



			Refresh();
		}

		public void PoolhierarchyEntry(HierarchyEntry hierarchyEntry)
		{
			PoolHierarchys.Enqueue(hierarchyEntry);
			hierarchyEntry.transform.SetParent(PoolHolder);
			hierarchyEntry.gameObject.SetActive(false);
			if (OpenHierarchys.Contains(hierarchyEntry)) OpenHierarchys.Remove(hierarchyEntry);
		}

		public void ClearHierarchy()
		{
			foreach (var hierarchyEntry in OpenHierarchys.ToArray())
			{
				if (hierarchyEntry.Shelf != null)
				{
					hierarchyEntry.ResetThis();
				}
			}
		}

		public void Refresh()
		{
			ClearHierarchy();
			IDtoHierarchyEntry.Clear();
			OpenHierarchys.Clear();
			HierarchyEntryRoots.Clear();

			foreach (var root in Roots)
			{
				HierarchyEntry HierarchyEntry = GethierarchyEntry();
				HierarchyEntry.transform.SetParent(RootSpace);
				HierarchyEntry.SetActive(true);
				HierarchyEntry.SetThis(root);
				HierarchyEntry.transform.localScale = Vector3.one;
				IDtoHierarchyEntry[root.ID] = HierarchyEntry;
				//RecursivPopulate(root);
			}

			if (UIManager.Instance.OrNull()?.UI_BooksInBookshelf.OrNull()?.BookShelfView?.ID == null) return;
			if (IDtoBookShelves.ContainsKey(UIManager.Instance.UI_BooksInBookshelf.BookShelfView.ID))
			{
				var bookShelf = IDtoBookShelves[UIManager.Instance.UI_BooksInBookshelf.BookShelfView.ID];
				List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> passlist =
					new List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf>();
				var top = RecursiveGetParent(bookShelf, passlist);
				if (top == null) return; //In case the selected one is actually the top of hierarchy
				passlist.Add(top.Shelf);
				passlist.Reverse();
				int Count = passlist.Count;
				int i = 0;
				foreach (var NFHBS in passlist)
				{
					if (IDtoHierarchyEntry.ContainsKey(NFHBS.ID) == false)
					{
						IDtoHierarchyEntry[NFHBS.PID].ChildPage = 0;
						IDtoHierarchyEntry[NFHBS.PID].DisplayPage();
						RecursiveUntilDisplayed(NFHBS);
					}
					IDtoHierarchyEntry[NFHBS.ID].ExpandChildren();
					i++;
					if (Count == i)
					{
						IDtoHierarchyEntry[NFHBS.ID].Highlight();
					}
				}

			}
		}

		public void RecursiveUntilDisplayed(VariableViewerNetworking.NetFriendlyHierarchyBookShelf NFHBS)
		{
			if (IDtoHierarchyEntry.ContainsKey(NFHBS.ID))
			{
				return;
			}
			else
			{
				if (IDtoHierarchyEntry[NFHBS.PID].ThePageDown.activeSelf)
				{
					IDtoHierarchyEntry[NFHBS.PID].PageDown();
					RecursiveUntilDisplayed(NFHBS);
				}
			}
		}

		public HierarchyEntry RecursiveGetParent(
				VariableViewerNetworking.NetFriendlyHierarchyBookShelf bookShelf,
				List<VariableViewerNetworking.NetFriendlyHierarchyBookShelf> passlist)
		{
			if (bookShelf.PID == 0) return null;
			passlist.Add(bookShelf);
			if (IDtoHierarchyEntry.ContainsKey(bookShelf.PID))
			{
				return IDtoHierarchyEntry[bookShelf.PID];
			}
			else
			{
				return RecursiveGetParent(IDtoBookShelves[bookShelf.PID], passlist);
			}
		}
	}
}
