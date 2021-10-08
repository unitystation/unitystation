using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Messages.Client.VariableViewer;


namespace AdminTools.VariableViewer
{
	public class HierarchyEntry : MonoBehaviour
	{
		public GameObject ThePageUp;
		public GameObject ThePageDown;

		public VariableViewerNetworking.NetFriendlyHierarchyBookShelf Shelf;

		public TMP_Text Name;
		public TMP_Text ExpandText;

		public int ChildPage = 0;

		public const int NumberPerPage = 60;

		public List<HierarchyEntry> hierarchyEntryChildren = new List<HierarchyEntry>();

		public void SetThis(VariableViewerNetworking.NetFriendlyHierarchyBookShelf _Shelf)
		{
			ThePageUp.SetActive(false);
			ThePageDown.SetActive(false);
			Shelf = _Shelf;
			Name.text = _Shelf.Nm;
		}

		public void Highlight()
		{
			Name.color = Color.yellow;
		}

		public void CleanPage()
		{
			foreach (var hierarchyEntry in hierarchyEntryChildren)
			{
				hierarchyEntry.ResetThis();
			}
			hierarchyEntryChildren.Clear();
			ThePageUp.SetActive(false);
			ThePageDown.SetActive(false);
		}

		public void DisplayPage()
		{
			CleanPage();
			ThePageUp.SetActive(false);
			ThePageDown.SetActive(false);

			int JumpNext = NumberPerPage;
			int Count = Shelf.GetChildrenList().Count;
			if (NumberPerPage * ChildPage + NumberPerPage > Count)
			{
				JumpNext = Count - NumberPerPage * ChildPage;
			}

			var TList = Shelf.GetChildrenList().GetRange(NumberPerPage * ChildPage, JumpNext);


			int i = 0;
			foreach (var Child in TList)
			{
				var hierarchyEntry = UIManager.Instance.LibraryUI.GethierarchyEntry();
				hierarchyEntryChildren.Add(hierarchyEntry);
				hierarchyEntry.transform.SetParent(this.transform);
				hierarchyEntry.transform.localScale = Vector3.one;
				hierarchyEntry.gameObject.SetActive(true);
				hierarchyEntry.SetThis(Child);
				UIManager.Instance.LibraryUI.IDtoHierarchyEntry[Child.ID] = hierarchyEntry;
				i++;
				if (i > NumberPerPage - 1)
				{
					ThePageDown.SetActive(true);
					ThePageDown.transform.SetAsLastSibling();
					break;
				}
			}

			if (ChildPage > 0)
			{
				ThePageUp.SetActive(true);
				ThePageUp.transform.SetSiblingIndex(1);
			}
		}

		public void PageDown()
		{
			ChildPage++;
			DisplayPage();
		}

		public void PageUp()
		{
			ChildPage--;
			DisplayPage();
		}

		public void ExpandChildren()
		{
			if (ExpandText.text == ">")
			{
				ExpandText.text = "\\/";
				DisplayPage();
			}
			else
			{
				ExpandText.text = ">";
				CleanPage();
			}

		}

		public void OpenShelf()
		{
			RequestBookshelfNetMessage.Send(Shelf.ID, true);
		}

		public void ResetThis()
		{
			Name.color = Color.white;
			ExpandText.text = ">";
			ChildPage = 0;
			gameObject.SetActive(false);
			ThePageUp.SetActive(false);
			ThePageDown.SetActive(false);
			UIManager.Instance.LibraryUI.PoolhierarchyEntry(this);
			Shelf = null;
			Name.text = "UnSET!";
			foreach (var hierarchyEntry in hierarchyEntryChildren)
			{
				hierarchyEntry.ResetThis();
			}
			hierarchyEntryChildren.Clear();
		}
	}
}
