using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class StringSearchList : ScriptableObject, ISearchWindowProvider
{

	private string[] Options;
	private Action<string> OnOptionSelected;

	public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
	{
		var SL = new List<SearchTreeEntry>();
		SL.Add(new SearchTreeGroupEntry(new GUIContent("Options"),0));


		List<string> SortedListItems = Options.ToList();

		SortedListItems.Sort((a, b) =>
		{
			string[] Split1 = a.Split('/');
			string[] Split2 = b.Split('/');
			for (int i = 0; i < Split1.Length; i++)
			{
				if (i >= Split2.Length)
				{
					return 1;
				}

				var Value = Split1[i].CompareTo(Split2[i]);
				if (Value != 0)
				{
					if (Split1.Length != Split2.Length && (i == Split1.Length - 1 || i == Split2.Length - 1))
					{
						return Split1.Length < Split2.Length ? 1 : -1;
					}

					return Value;
				}
			}
			return 0;
		});

		var Groups = new List<string>();

		foreach (var Item in SortedListItems)
		{
			string[] EntryTitle = Item.Split('/');
			string Groupname = "";
			
			for (int i = 0; i < EntryTitle.Length-1; i++)
			{

				Groupname += EntryTitle[i];
				if (Groups.Contains(Groupname) == false)
				{
					SL.Add(new SearchTreeGroupEntry(new GUIContent(EntryTitle[i]), i + 1));
					Groups.Add(Groupname);
				}

				Groupname += "/";
			}

			var Entry = new SearchTreeEntry(new GUIContent(EntryTitle.Last()))
			{
				level = EntryTitle.Length,
				userData = Item
			};
			SL.Add(Entry);
		}

		return SL;
	}

    public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
    {
	    OnOptionSelected?.Invoke((string)SearchTreeEntry.userData);
	    return true;

    }

    public StringSearchList(string[] newOptions, Action<string> InOnOptionSelected)
    {
	    Options = newOptions.ToArray();
	    OnOptionSelected = InOnOptionSelected;
    }

}
